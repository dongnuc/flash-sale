using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Orders;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Persistence.Repositories;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Queries;

internal sealed class OrderQueries : SqlRepositoryBase, IOrderQueries
{
    public OrderQueries(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public async Task<PagedResult<OrderSummaryDto>> GetCustomerHistoryAsync(
        Guid userId, Domain.Enums.OrderStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.Id AS OrderId,
                   CONCAT('FS-', CONVERT(char(8), o.CreatedAt, 112), '-', RIGHT(REPLACE(CONVERT(varchar(36), o.Id), '-', ''), 8)) AS OrderCode,
                   CASE o.Status WHEN 0 THEN 'Pending' WHEN 1 THEN 'Paid' WHEN 2 THEN 'Shipping'
                        WHEN 3 THEN 'Completed' WHEN 4 THEN 'Cancelled' WHEN 5 THEN 'Expired' END AS Status,
                   CASE WHEN payment.Provider IS NULL THEN NULL ELSE 'ONLINE' END AS PaymentMethod,
                   payment.Provider AS PaymentProvider, payment.PaymentStatus,
                   o.TotalAmount, o.CreatedAt, o.ExpiresAt AS PaymentDeadline
            FROM dbo.Orders AS o
            OUTER APPLY
            (
                SELECT TOP (1) p.Provider,
                       CASE p.Status WHEN 0 THEN 'Pending' WHEN 1 THEN 'Paid' WHEN 2 THEN 'Failed'
                            WHEN 3 THEN 'Expired' WHEN 4 THEN 'Cancelled' END AS PaymentStatus
                FROM dbo.Payments AS p
                WHERE p.OrderId = o.Id
                ORDER BY p.CreatedAt DESC, p.Id DESC
            ) AS payment
            WHERE o.UserId = @UserId AND (@Status IS NULL OR o.Status = @Status)
            """;

        var page = await QueryPagedAsync<OrderSummaryRow>(sql, "CreatedAt DESC, OrderId DESC",
            new { UserId = userId, Status = status }, pageIndex, pageSize, cancellationToken);
        return MapPage(page);
    }

    public async Task<OrderDetailDto?> GetCustomerDetailAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await GetDetailRowAsync(orderId, userId, cancellationToken);
        if (row is null) return null;

        return MapDetail(row, await GetLinesAsync(orderId, cancellationToken));
    }

    public async Task<PagedResult<OrderSummaryDto>> GetAdminPageAsync(OrderAdminFilter filter, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.Id AS OrderId,
                   CONCAT('FS-', CONVERT(char(8), o.CreatedAt, 112), '-', RIGHT(REPLACE(CONVERT(varchar(36), o.Id), '-', ''), 8)) AS OrderCode,
                   CASE o.Status WHEN 0 THEN 'Pending' WHEN 1 THEN 'Paid' WHEN 2 THEN 'Shipping'
                        WHEN 3 THEN 'Completed' WHEN 4 THEN 'Cancelled' WHEN 5 THEN 'Expired' END AS Status,
                   CASE WHEN payment.Provider IS NULL THEN NULL ELSE 'ONLINE' END AS PaymentMethod,
                   payment.Provider AS PaymentProvider, payment.PaymentStatus,
                   o.TotalAmount, o.CreatedAt, o.ExpiresAt AS PaymentDeadline
            FROM dbo.Orders AS o
            INNER JOIN dbo.Users AS u ON u.Id = o.UserId
            OUTER APPLY
            (
                SELECT TOP (1) p.Provider,
                       CASE p.Status WHEN 0 THEN 'Pending' WHEN 1 THEN 'Paid' WHEN 2 THEN 'Failed'
                            WHEN 3 THEN 'Expired' WHEN 4 THEN 'Cancelled' END AS PaymentStatus
                FROM dbo.Payments AS p
                WHERE p.OrderId = o.Id
                ORDER BY p.CreatedAt DESC, p.Id DESC
            ) AS payment
            WHERE (@SearchUser IS NULL OR u.FullName LIKE N'%' + @SearchUser + N'%'
                   OR u.UserName LIKE N'%' + @SearchUser + N'%'
                   OR u.Email LIKE N'%' + @SearchUser + N'%')
              AND (@Status IS NULL OR o.Status = @Status)
              AND (@MinTotalPrice IS NULL OR o.TotalAmount >= @MinTotalPrice)
              AND (@MaxTotalPrice IS NULL OR o.TotalAmount <= @MaxTotalPrice)
            """;
        var parameters = new
        {
            SearchUser = string.IsNullOrWhiteSpace(filter.SearchUser) ? null : filter.SearchUser.Trim(),
            filter.Status,
            filter.MinTotalPrice,
            filter.MaxTotalPrice
        };
        var orderBy = filter.SortTotalAscending ? "TotalAmount ASC, CreatedAt DESC, OrderId" : "TotalAmount DESC, CreatedAt DESC, OrderId";
        var page = await QueryPagedAsync<OrderSummaryRow>(sql, orderBy, parameters,
            filter.PageIndex, filter.PageSize, cancellationToken);
        return MapPage(page);
    }

    public async Task<StoreOrderDetailDto?> GetAdminDetailAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var row = await GetDetailRowAsync(orderId, null, cancellationToken);
        if (row is null) return null;

        var detail = MapDetail(row, await GetLinesAsync(orderId, cancellationToken));
        return new StoreOrderDetailDto
        {
            OrderId = detail.OrderId,
            OrderCode = detail.OrderCode,
            Status = detail.Status,
            PaymentMethod = detail.PaymentMethod,
            PaymentProvider = detail.PaymentProvider,
            PaymentStatus = detail.PaymentStatus,
            TotalAmount = detail.TotalAmount,
            CreatedAt = detail.CreatedAt,
            PaymentDeadline = detail.PaymentDeadline,
            Items = detail.Items,
            Subtotal = detail.Subtotal,
            ShippingFee = detail.ShippingFee,
            Note = detail.Note,
            DeliveryAddress = detail.DeliveryAddress,
            UserId = row.UserId,
            UserName = row.UserName ?? string.Empty,
            Email = row.Email,
            ProviderTransactionId = row.ProviderTransactionId,
            PaymentAmount = row.PaymentAmount
        };
    }

    private Task<OrderDetailRow?> GetDetailRowAsync(Guid orderId, Guid? userId, CancellationToken cancellationToken) =>
        QueryFirstOrDefaultAsync<OrderDetailRow>("""
            SELECT o.Id AS OrderId, o.UserId,
                   CONCAT('FS-', CONVERT(char(8), o.CreatedAt, 112), '-', RIGHT(REPLACE(CONVERT(varchar(36), o.Id), '-', ''), 8)) AS OrderCode,
                   CASE o.Status WHEN 0 THEN 'Pending' WHEN 1 THEN 'Paid' WHEN 2 THEN 'Shipping'
                        WHEN 3 THEN 'Completed' WHEN 4 THEN 'Cancelled' WHEN 5 THEN 'Expired' END AS Status,
                   CASE WHEN payment.Provider IS NULL THEN NULL ELSE 'ONLINE' END AS PaymentMethod,
                   payment.Provider AS PaymentProvider, payment.PaymentStatus, o.TotalAmount, o.CreatedAt,
                   o.ExpiresAt AS PaymentDeadline, o.ReceiverName, o.ReceiverPhone, o.ShippingAddress,
                   o.Province, o.District, o.Ward, o.AddressLine, o.Note,
                   u.UserName, u.Email, payment.ProviderTransactionId, payment.Amount AS PaymentAmount
            FROM dbo.Orders AS o
            INNER JOIN dbo.Users AS u ON u.Id = o.UserId
            OUTER APPLY
            (
                SELECT TOP (1) p.Provider, p.ProviderTransactionId, p.Amount,
                       CASE p.Status WHEN 0 THEN 'Pending' WHEN 1 THEN 'Paid' WHEN 2 THEN 'Failed'
                            WHEN 3 THEN 'Expired' WHEN 4 THEN 'Cancelled' END AS PaymentStatus
                FROM dbo.Payments AS p
                WHERE p.OrderId = o.Id
                ORDER BY p.CreatedAt DESC, p.Id DESC
            ) AS payment
            WHERE o.Id = @OrderId AND (@UserId IS NULL OR o.UserId = @UserId);
            """, new { OrderId = orderId, UserId = userId }, cancellationToken);

    private async Task<IReadOnlyList<OrderLineDto>> GetLinesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var rows = await QueryAsync<OrderLineRow>("""
            SELECT od.ProductId, od.ProductName,
                   CASE WHEN od.FlashSaleItemId IS NULL THEN 'REGULAR' ELSE 'FLASH_SALE' END AS PriceType,
                   fsi.FlashSaleId, od.Quantity, od.UnitPrice, od.LineTotal
            FROM dbo.OrderDetails AS od
            LEFT JOIN dbo.FlashSaleItems AS fsi ON fsi.Id = od.FlashSaleItemId
            WHERE od.OrderId = @OrderId
            ORDER BY od.CreatedAt, od.Id;
            """, new { OrderId = orderId }, cancellationToken);
        return rows.Select(row => new OrderLineDto
        {
            ProductId = row.ProductId,
            ProductName = row.ProductName,
            PriceType = row.PriceType,
            FlashSaleId = row.FlashSaleId,
            Quantity = row.Quantity,
            UnitPrice = row.UnitPrice,
            LineTotal = row.LineTotal
        }).ToArray();
    }

    private static PagedResult<OrderSummaryDto> MapPage(PagedResult<OrderSummaryRow> page) =>
        new(page.Items.Select(MapSummary).ToList(), page.PageIndex, page.PageSize, page.TotalCount);

    private static OrderSummaryDto MapSummary(OrderSummaryRow row) => new()
    {
        OrderId = row.OrderId,
        OrderCode = row.OrderCode,
        Status = row.Status,
        PaymentMethod = row.PaymentMethod,
        PaymentProvider = row.PaymentProvider,
        PaymentStatus = row.PaymentStatus,
        TotalAmount = row.TotalAmount,
        CreatedAt = row.CreatedAt,
        PaymentDeadline = row.PaymentDeadline
    };

    private static OrderDetailDto MapDetail(OrderDetailRow row, IReadOnlyList<OrderLineDto> lines) => new()
    {
        OrderId = row.OrderId,
        OrderCode = row.OrderCode,
        Status = row.Status,
        PaymentMethod = row.PaymentMethod,
        PaymentStatus = row.PaymentStatus,
        TotalAmount = row.TotalAmount,
        CreatedAt = row.CreatedAt,
        PaymentDeadline = row.PaymentDeadline,
        Items = lines,
        Subtotal = row.TotalAmount,
        ShippingFee = 0,
        Note = row.Note,
        DeliveryAddress = new DeliveryAddressDto
        {
            RecipientName = row.ReceiverName ?? string.Empty,
            Phone = row.ReceiverPhone ?? string.Empty,
            Province = row.Province,
            District = row.District,
            Ward = row.Ward,
            AddressLine = row.AddressLine ?? row.ShippingAddress ?? string.Empty
        }
    };

    private class OrderSummaryRow
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PaymentProvider { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaymentDeadline { get; set; }
    }

    private sealed class OrderDetailRow : OrderSummaryRow
    {
        public Guid UserId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? AddressLine { get; set; }
        public string? Note { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? ProviderTransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }
    }

    private sealed class OrderLineRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string PriceType { get; set; } = string.Empty;
        public Guid? FlashSaleId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
