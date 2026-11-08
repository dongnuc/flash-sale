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

namespace FlashSaleStore.Application.UseCases.Catalog.Categories.Commands;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository repository,
    ICategoryQueries queries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<AdminCategoryDto>>
{
    public async Task<Result<AdminCategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var duplicate = await repository.GetByNormalizedNameAsync(
                    CatalogUseCaseErrors.NormalizeName(request.Name), transactionToken);
                if (duplicate is not null)
                {
                    return Result.Failure<AdminCategoryDto>(CatalogUseCaseErrors.CategoryNameDuplicate(request.Name.Trim()));
                }

                var category = Category.Create(request.Name, request.Description);
                if (await repository.InsertAsync(category, transactionToken) != 1)
                {
                    throw new DomainRuleViolationException("Category.CreateFailed", "Không thể tạo category.");
                }

                var output = await queries.GetAdminByIdAsync(category.Id, transactionToken)
                    ?? throw new InvalidOperationException("Category was inserted but could not be read back.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<AdminCategoryDto>(CatalogUseCaseErrors.FromDomain(exception));
        }
    }
}

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository repository,
    ICategoryQueries queries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result<AdminCategoryDto>>
{
    public async Task<Result<AdminCategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var category = await repository.GetByIdAsync(request.CategoryId, transactionToken);
                if (category is null)
                {
                    return Result.Failure<AdminCategoryDto>(CatalogUseCaseErrors.CategoryNotFound(request.CategoryId));
                }

                var duplicate = await repository.GetByNormalizedNameAsync(
                    CatalogUseCaseErrors.NormalizeName(request.Name), transactionToken);
                if (duplicate is not null && duplicate.Id != category.Id)
                {
                    return Result.Failure<AdminCategoryDto>(CatalogUseCaseErrors.CategoryNameDuplicate(request.Name.Trim()));
                }

                category.UpdateDetails(request.Name, request.Description);
                if (await repository.UpdateAsync(category, transactionToken) != 1)
                {
                    return Result.Failure<AdminCategoryDto>(CatalogUseCaseErrors.CategoryNotFound(request.CategoryId));
                }

                var output = await queries.GetAdminByIdAsync(category.Id, transactionToken)
                    ?? throw new InvalidOperationException("Updated category could not be read back.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<AdminCategoryDto>(CatalogUseCaseErrors.FromDomain(exception));
        }
    }
}

public sealed class BatchUpdateCategoryStatusCommandHandler(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BatchUpdateCategoryStatusCommand, Result<BatchStatusDto>>
{
    public async Task<Result<BatchStatusDto>> Handle(BatchUpdateCategoryStatusCommand request, CancellationToken cancellationToken)
    {
        if (!CatalogUseCaseErrors.TryParseActiveStatus(request.Status, out var isActive))
        {
            return Result.Failure<BatchStatusDto>(new Error("Category.Status.Invalid", "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE."));
        }

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var categories = await repository.GetByIdsAsync(request.Ids, transactionToken);
                if (categories.Count != request.Ids.Length)
                {
                    var foundIds = categories.Select(category => category.Id).ToHashSet();
                    var missingId = request.Ids.First(id => !foundIds.Contains(id));
                    return Result.Failure<BatchStatusDto>(CatalogUseCaseErrors.CategoryNotFound(missingId));
                }

                foreach (var category in categories)
                {
                    category.SetActive(isActive);
                    if (await repository.UpdateAsync(category, transactionToken) != 1)
                    {
                        throw new DomainRuleViolationException(
                            "Category.BatchUpdateFailed",
                            $"Không thể cập nhật category '{category.Id}'; toàn bộ batch đã rollback.",
                            DomainFailureKind.Conflict);
                    }
                }

                return Result.Success(new BatchStatusDto
                {
                    RequestedCount = request.Ids.Length,
                    UpdatedCount = categories.Count,
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
