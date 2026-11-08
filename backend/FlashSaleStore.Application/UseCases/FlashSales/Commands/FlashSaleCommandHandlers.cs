using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;
using FlashSaleStore.Domain.Services;
using MediatR;

namespace FlashSaleStore.Application.UseCases.FlashSales.Commands;

public sealed class CreateFlashSaleCommandHandler(IFlashSaleRepository sales, IFlashSaleItemRepository items, IProductRepository products, IFlashSaleQueries queries, IUnitOfWork unitOfWork, FlashSaleSchedulePolicy schedulePolicy)
    : IRequestHandler<CreateFlashSaleCommand, Result<FlashSaleDto>>
{
    public async Task<Result<FlashSaleDto>> Handle(CreateFlashSaleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var duplicateProducts = request.Items.GroupBy(item => item.ProductId).FirstOrDefault(group => group.Count() > 1);
                if (duplicateProducts is not null) return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.Invalid("FlashSaleItem.Product.Duplicate", "Mỗi sản phẩm chỉ được xuất hiện một lần trong FlashSale."));
                var sale = FlashSale.Create(request.Name, request.StartsAt, request.EndsAt);
                var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
                var productList = await products.GetByIdsAsync(productIds, ct);
                if (productList.Count != productIds.Length)
                {
                    var found = productList.Select(product => product.Id).ToHashSet();
                    return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.NotFound("Product", productIds.First(id => !found.Contains(id))));
                }
                var existing = await items.GetSchedulesByProductIdsAsync(productIds, ct);
                var createdItems = new List<FlashSaleItem>(request.Items.Count);
                foreach (var input in request.Items)
                {
                    var product = productList.Single(x => x.Id == input.ProductId);
                    var held = await products.GetQuantityHeldByPendingOrdersAsync(product.Id, ct);
                    var total = checked(product.StockQuantity + held);
                    var item = FlashSaleItem.Create(sale.Id, product.Id, input.FinalPrice, product.Price, input.FlashSaleQuantity, total, input.LimitPerUser);
                    schedulePolicy.EnsureNoOverlap(new(sale.Id, product.Id, request.StartsAt, request.EndsAt, sale.Status), existing);
                    createdItems.Add(item);
                }
                if (await sales.InsertAsync(sale, ct) != 1) throw new DomainRuleViolationException("FlashSale.CreateFailed", "Không thể tạo FlashSale.");
                foreach (var item in createdItems)
                    if (await items.InsertAsync(item, ct) != 1) throw new DomainRuleViolationException("FlashSaleItem.CreateFailed", "Không thể lưu mặt hàng FlashSale.");
                var output = await queries.GetByIdAsync(sale.Id, ct) ?? throw new InvalidOperationException("FlashSale insert could not be read back.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.FromDomain(ex)); }
        catch (OverflowException) { return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.Invalid("FlashSale.Stock.Overflow", "Tồn kho vượt giới hạn cho phép.")); }
    }
}

public sealed class UpdateFlashSaleCommandHandler(IFlashSaleRepository sales, IFlashSaleItemRepository items, IProductRepository products, IFlashSaleQueries queries, IUnitOfWork unitOfWork, FlashSaleSchedulePolicy schedulePolicy)
    : IRequestHandler<UpdateFlashSaleCommand, Result<FlashSaleDto>>
{
    public async Task<Result<FlashSaleDto>> Handle(UpdateFlashSaleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var sale = await sales.GetByIdAsync(request.FlashSaleId, ct);
                if (sale is null) return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.NotFound("FlashSale", request.FlashSaleId));
                if (request.Items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
                    return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.Invalid("FlashSaleItem.Product.Duplicate", "Mỗi sản phẩm chỉ được xuất hiện một lần trong FlashSale."));
                var oldItems = await items.GetByFlashSaleIdAsync(sale.Id, ct);
                var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
                var productList = await products.GetByIdsAsync(productIds, ct);
                if (productList.Count != productIds.Length)
                {
                    var found = productList.Select(product => product.Id).ToHashSet();
                    return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.NotFound("Product", productIds.First(id => !found.Contains(id))));
                }
                var schedules = await items.GetSchedulesByProductIdsAsync(productIds, ct);
                var additions = new List<FlashSaleItem>();
                var updates = new List<FlashSaleItem>();
                foreach (var input in request.Items)
                {
                    var product = productList.Single(x => x.Id == input.ProductId);
                    var held = await products.GetQuantityHeldByPendingOrdersAsync(product.Id, ct);
                    var total = checked(product.StockQuantity + held);
                    var existingItem = oldItems.SingleOrDefault(x => x.ProductId == product.Id);
                    if (existingItem is null)
                    {
                        existingItem = FlashSaleItem.Create(sale.Id, product.Id, input.FinalPrice, product.Price, input.FlashSaleQuantity, total, input.LimitPerUser);
                        additions.Add(existingItem);
                    }
                    else
                    {
                        existingItem.UpdateOffer(input.FinalPrice, product.Price, input.FlashSaleQuantity, total, input.LimitPerUser);
                        updates.Add(existingItem);
                    }
                    schedulePolicy.EnsureNoOverlap(new(sale.Id, product.Id, request.StartsAt, request.EndsAt, sale.Status), schedules);
                }
                var requestedIds = productIds.ToHashSet();
                var removedItems = oldItems.Where(old => !requestedIds.Contains(old.ProductId)).ToArray();
                if (removedItems.Any(item => item.SoldQuantity > 0))
                    return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.Invalid("FlashSaleItem.Quantity.HasReservations", "Không thể xóa sản phẩm đã có quota được giữ hoặc đã bán."));
                sale.UpdateDetails(request.Name, request.StartsAt, request.EndsAt);
                if (await sales.UpdateAsync(sale, ct) != 1) throw new DomainRuleViolationException("FlashSale.ConcurrencyConflict", "FlashSale đã được cập nhật bởi thao tác khác.", DomainFailureKind.Conflict);
                foreach (var item in updates)
                    if (await items.UpdateAsync(item, ct) != 1) throw new DomainRuleViolationException("FlashSaleItem.ConcurrencyConflict", "FlashSale item đã thay đổi; hãy tải lại.", DomainFailureKind.Conflict);
                foreach (var item in additions)
                    if (await items.InsertAsync(item, ct) != 1) throw new DomainRuleViolationException("FlashSaleItem.CreateFailed", "Không thể thêm sản phẩm vào FlashSale.");
                foreach (var removed in removedItems)
                    if (await items.DeleteAsync(removed.Id, ct) != 1) throw new DomainRuleViolationException("FlashSaleItem.DeleteFailed", "Không thể xóa sản phẩm khỏi FlashSale.");
                var output = await queries.GetByIdAsync(sale.Id, ct) ?? throw new InvalidOperationException("Updated FlashSale could not be read back.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.FromDomain(ex)); }
        catch (OverflowException) { return Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.Invalid("FlashSale.Stock.Overflow", "Tồn kho vượt giới hạn cho phép.")); }
    }
}

public sealed class BatchUpdateFlashSaleStatusCommandHandler(IFlashSaleRepository sales, IFlashSaleItemRepository items, IUnitOfWork unitOfWork, FlashSaleSchedulePolicy schedulePolicy)
    : IRequestHandler<BatchUpdateFlashSaleStatusCommand, Result<BatchStatusDto>>
{
    public async Task<Result<BatchStatusDto>> Handle(BatchUpdateFlashSaleStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FlashSaleStatus>(request.Status, true, out var target)) return Result.Failure<BatchStatusDto>(CommerceUseCaseErrors.Invalid("FlashSale.Status.Invalid", "Trạng thái FlashSale không hợp lệ."));
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var found = await sales.GetByIdsAsync(request.Ids, ct);
                if (found.Count != request.Ids.Length)
                {
                    var foundIds = found.Select(x => x.Id).ToHashSet();
                    return Result.Failure<BatchStatusDto>(CommerceUseCaseErrors.NotFound("FlashSale", request.Ids.First(x => !foundIds.Contains(x))));
                }
                var allItems = new List<FlashSaleItem>();
                foreach (var sale in found) allItems.AddRange(await items.GetByFlashSaleIdAsync(sale.Id, ct));
                var productIds = allItems.Select(x => x.ProductId).Distinct().ToArray();
                var schedules = await items.GetSchedulesByProductIdsAsync(productIds, ct);
                var now = DateTime.UtcNow;
                foreach (var sale in found)
                {
                    sale.ChangeStatus(target, now);
                    if (target is FlashSaleStatus.Scheduled or FlashSaleStatus.Active)
                        foreach (var item in allItems.Where(x => x.FlashSaleId == sale.Id))
                            schedulePolicy.EnsureNoOverlap(new(sale.Id, item.ProductId, sale.StartsAt, sale.EndsAt, target), schedules);
                    if (await sales.UpdateAsync(sale, ct) != 1) throw new DomainRuleViolationException("FlashSale.BatchUpdateFailed", $"Không thể cập nhật FlashSale '{sale.Id}'; toàn bộ batch đã rollback.", DomainFailureKind.Conflict);
                }
                return Result.Success(new BatchStatusDto { RequestedCount = request.Ids.Length, UpdatedCount = found.Count, UpdatedIds = request.Ids, Status = target.ToString().ToUpperInvariant() });
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<BatchStatusDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}

public sealed class SetFlashSaleStartTimeCommandHandler(IFlashSaleRepository sales, IFlashSaleItemRepository items, IUnitOfWork unitOfWork, FlashSaleSchedulePolicy schedulePolicy)
    : IRequestHandler<SetFlashSaleStartTimeCommand, Result<FlashSaleStartTimeDto>>
{
    public async Task<Result<FlashSaleStartTimeDto>> Handle(SetFlashSaleStartTimeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var sale = await sales.GetByIdAsync(request.FlashSaleId, ct);
                if (sale is null) return Result.Failure<FlashSaleStartTimeDto>(CommerceUseCaseErrors.NotFound("FlashSale", request.FlashSaleId));
                var saleItems = await items.GetByFlashSaleIdAsync(sale.Id, ct);
                var schedules = await items.GetSchedulesByProductIdsAsync(saleItems.Select(x => x.ProductId).ToArray(), ct);
                var startDate = DateTime.SpecifyKind(request.StartsAt, DateTimeKind.Utc);
                foreach (var item in saleItems)
                    schedulePolicy.EnsureNoOverlap(new(sale.Id, item.ProductId, startDate, sale.EndsAt, sale.Status), schedules);
                sale.SetStartTime(startDate);
                if (await sales.UpdateAsync(sale, ct) != 1) throw new DomainRuleViolationException("FlashSale.ConcurrencyConflict", "FlashSale đã được cập nhật bởi thao tác khác.", DomainFailureKind.Conflict);
                return Result.Success(new FlashSaleStartTimeDto(sale.Id, sale.StartsAt));
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<FlashSaleStartTimeDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}
