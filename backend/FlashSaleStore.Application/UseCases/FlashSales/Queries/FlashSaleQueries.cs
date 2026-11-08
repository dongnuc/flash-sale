using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Enums;
using FluentValidation;
using MediatR;

namespace FlashSaleStore.Application.UseCases.FlashSales.Queries;

public sealed record GetAdminFlashSalesQuery(DateTime? DateFrom, DateTime? DateTo, string? Search, string? Status, int PageIndex, int PageSize) : IRequest<Result<PagedResult<FlashSaleDto>>>;
public sealed record GetAdminFlashSaleDetailQuery(Guid FlashSaleId) : IRequest<Result<FlashSaleDto>>;

public sealed class GetAdminFlashSalesQueryValidator : AbstractValidator<GetAdminFlashSalesQuery>
{
    public GetAdminFlashSalesQueryValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(x => x is null || Enum.GetNames<FlashSaleStatus>().Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Trạng thái FlashSale không hợp lệ.");
        RuleFor(x => x.DateFrom).Must(x => x is null || x.Value.Kind == DateTimeKind.Utc)
            .WithMessage("dateFrom phải có múi giờ UTC (đuôi Z).");
        RuleFor(x => x.DateTo).Must(x => x is null || x.Value.Kind == DateTimeKind.Utc)
            .WithMessage("dateTo phải có múi giờ UTC (đuôi Z).");
        RuleFor(x => x).Must(x => x.DateFrom is null || x.DateTo is null || x.DateFrom <= x.DateTo)
            .WithMessage("dateFrom phải nhỏ hơn hoặc bằng dateTo.");
    }
}
public sealed class GetAdminFlashSaleDetailQueryValidator : AbstractValidator<GetAdminFlashSaleDetailQuery>
{
    public GetAdminFlashSaleDetailQueryValidator() => RuleFor(x => x.FlashSaleId).NotEmpty();
}

public sealed class GetAdminFlashSalesQueryHandler(IFlashSaleQueries queries) : IRequestHandler<GetAdminFlashSalesQuery, Result<PagedResult<FlashSaleDto>>>
{
    public async Task<Result<PagedResult<FlashSaleDto>>> Handle(GetAdminFlashSalesQuery request, CancellationToken cancellationToken)
    {
        if (request.Status is not null && !Enum.GetNames<FlashSaleStatus>().Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            return Result.Failure<PagedResult<FlashSaleDto>>(CommerceUseCaseErrors.Invalid("FlashSale.Status.Invalid", "Trạng thái FlashSale không hợp lệ."));
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
            return Result.Failure<PagedResult<FlashSaleDto>>(CommerceUseCaseErrors.Invalid("FlashSale.DateRange.Invalid", "dateFrom phải nhỏ hơn hoặc bằng dateTo."));
        FlashSaleStatus? status = request.Status is null ? null : Enum.Parse<FlashSaleStatus>(request.Status, true);
        var page = await queries.GetAdminPageAsync(new(request.DateFrom, request.DateTo, request.Search, status, request.PageIndex, request.PageSize), cancellationToken);
        return Result.Success(page);
    }
}

public sealed class GetAdminFlashSaleDetailQueryHandler(IFlashSaleQueries queries) : IRequestHandler<GetAdminFlashSaleDetailQuery, Result<FlashSaleDto>>
{
    public async Task<Result<FlashSaleDto>> Handle(GetAdminFlashSaleDetailQuery request, CancellationToken cancellationToken)
    {
        var sale = await queries.GetByIdAsync(request.FlashSaleId, cancellationToken);
        return sale is null ? Result.Failure<FlashSaleDto>(CommerceUseCaseErrors.NotFound("FlashSale", request.FlashSaleId)) : Result.Success(sale);
    }
}
