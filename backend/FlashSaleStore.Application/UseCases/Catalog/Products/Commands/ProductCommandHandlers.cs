using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Application.UseCases.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Exceptions;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Products.Commands;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductQueries productQueries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!CatalogUseCaseErrors.TryParseActiveStatus(request.Status, out var isActive))
        {
            return Result.Failure<AdminProductDto>(new Error("Product.Status.Invalid", "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE."));
        }

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                if (await categoryRepository.GetByIdAsync(request.CategoryId, transactionToken) is null)
                {
                    return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.ProductCategoryNotFound(request.CategoryId));
                }

                var duplicate = await productRepository.GetByNormalizedNameAsync(
                    CatalogUseCaseErrors.NormalizeName(request.Name), transactionToken);
                if (duplicate is not null)
                {
                    return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.ProductNameDuplicate(request.Name.Trim()));
                }

                var product = Product.Create(
                    request.CategoryId,
                    request.Name,
                    request.Description,
                    request.RegularPrice,
                    request.Quantity,
                    request.IsFeatured);
                product.SetActive(isActive);

                if (await productRepository.InsertAsync(product, transactionToken) != 1)
                {
                    throw new DomainRuleViolationException("Product.CreateFailed", "Không thể tạo product.");
                }

                var output = await productQueries.GetAdminByIdAsync(product.Id, transactionToken)
                    ?? throw new InvalidOperationException("Product was inserted but could not be read back.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.FromDomain(exception));
        }
    }
}

public sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductQueries productQueries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (!CatalogUseCaseErrors.TryParseActiveStatus(request.Status, out var isActive))
        {
            return Result.Failure<AdminProductDto>(new Error("Product.Status.Invalid", "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE."));
        }

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var product = await productRepository.GetByIdAsync(request.ProductId, transactionToken);
                if (product is null)
                {
                    return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.ProductNotFound(request.ProductId));
                }

                if (await categoryRepository.GetByIdAsync(request.CategoryId, transactionToken) is null)
                {
                    return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.ProductCategoryNotFound(request.CategoryId));
                }

                var duplicate = await productRepository.GetByNormalizedNameAsync(
                    CatalogUseCaseErrors.NormalizeName(request.Name), transactionToken);
                if (duplicate is not null && duplicate.Id != product.Id)
                {
                    return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.ProductNameDuplicate(request.Name.Trim()));
                }

                var heldQuantity = await productRepository.GetQuantityHeldByPendingOrdersAsync(product.Id, transactionToken);
                product.UpdateDetails(request.CategoryId, request.Name, request.Description);
                product.ChangePrice(request.RegularPrice);
                product.SetStock(request.Quantity, heldQuantity);
                product.SetFeatured(request.IsFeatured);
                product.SetActive(isActive);

                if (await productRepository.UpdateAsync(product, transactionToken) != 1)
                {
                    throw new DomainRuleViolationException(
                        "Product.ConcurrencyConflict",
                        "Product đã được thay đổi bởi một thao tác khác; hãy tải lại dữ liệu và thử lại.",
                        DomainFailureKind.Conflict);
                }

                var output = await productQueries.GetAdminByIdAsync(product.Id, transactionToken)
                    ?? throw new InvalidOperationException("Updated product could not be read back.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<AdminProductDto>(CatalogUseCaseErrors.FromDomain(exception));
        }
    }
}

public sealed class BatchUpdateProductStatusCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BatchUpdateProductStatusCommand, Result<BatchStatusDto>>
{
    public async Task<Result<BatchStatusDto>> Handle(BatchUpdateProductStatusCommand request, CancellationToken cancellationToken)
    {
        if (!CatalogUseCaseErrors.TryParseActiveStatus(request.Status, out var isActive))
        {
            return Result.Failure<BatchStatusDto>(new Error("Product.Status.Invalid", "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE."));
        }

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var products = await repository.GetByIdsAsync(request.Ids, transactionToken);
                if (products.Count != request.Ids.Length)
                {
                    var foundIds = products.Select(product => product.Id).ToHashSet();
                    var missingId = request.Ids.First(id => !foundIds.Contains(id));
                    return Result.Failure<BatchStatusDto>(CatalogUseCaseErrors.ProductNotFound(missingId));
                }

                foreach (var product in products)
                {
                    product.SetActive(isActive);
                    if (await repository.UpdateAsync(product, transactionToken) != 1)
                    {
                        throw new DomainRuleViolationException(
                            "Product.BatchUpdateFailed",
                            $"Không thể cập nhật product '{product.Id}'; toàn bộ batch đã rollback.",
                            DomainFailureKind.Conflict);
                    }
                }

                return Result.Success(new BatchStatusDto
                {
                    RequestedCount = request.Ids.Length,
                    UpdatedCount = products.Count,
                    UpdatedIds = request.Ids,
                    Status = isActive ? "ACTIVE" : "INACTIVE"
                });
            }, cancellationToken);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<BatchStatusDto>(CatalogUseCaseErrors.FromDomain(exception));
        }
    }
}
