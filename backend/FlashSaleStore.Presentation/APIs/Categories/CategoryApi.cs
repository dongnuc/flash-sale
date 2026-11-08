using Carter;
using FlashSaleStore.Application.UseCases.Catalog.Categories.Commands;
using FlashSaleStore.Application.UseCases.Catalog.Categories.Queries;
using FlashSaleStore.Presentation.Abstractions;
using FlashSaleStore.Presentation.Contracts.Catalog;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlashSaleStore.Presentation.APIs.Categories;

public sealed class CategoryApi : ApiEndpoint, ICarterModule
{
    private const string CategoriesUrl = "/api/v{version:apiVersion}/categories";
    private const string AdminCategoriesUrl = "/api/v{version:apiVersion}/admin/categories";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var categories = app.NewVersionedApi("categories")
            .MapGroup(CategoriesUrl)
            .HasApiVersion(1);

        categories.MapGet("/", GetActiveCategories)
            .WithName("GetActiveCategories")
            .WithTags("Categories")
            .Produces(StatusCodes.Status200OK);

        var adminCategories = app.NewVersionedApi("admin-categories")
            .MapGroup(AdminCategoriesUrl)
            .HasApiVersion(1)
            .RequireAuthorization("StoreOwner");

        adminCategories.MapGet("/", GetAdminCategories)
            .WithName("GetAdminCategories")
            .WithTags("Admin Categories")
            .Produces(StatusCodes.Status200OK);

        adminCategories.MapPost("/", CreateCategory)
            .WithName("CreateCategory")
            .WithTags("Admin Categories")
            .Produces(StatusCodes.Status201Created);

        adminCategories.MapPut("/{categoryId:guid}", UpdateCategory)
            .WithName("UpdateCategory")
            .WithTags("Admin Categories")
            .Produces(StatusCodes.Status200OK);

        adminCategories.MapPatch("/batch-status", BatchUpdateCategoryStatus)
            .WithName("BatchUpdateCategoryStatus")
            .WithTags("Admin Categories")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetActiveCategories(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetActiveCategoriesQuery(), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetAdminCategories(
        ISender sender,
        string? search = null,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAdminCategoriesQuery(search, pageIndex, pageSize), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);
        return Handle(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateCategory(
        Guid categoryId,
        UpdateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateCategoryCommand(categoryId, request.Name, request.Description), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> BatchUpdateCategoryStatus(
        BatchStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BatchUpdateCategoryStatusCommand(request.Ids, request.Status), cancellationToken);
        return Handle(result);
    }
}
