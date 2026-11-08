using Carter;
using FlashSaleStore.Application.UseCases.Catalog.Products.Commands;
using FlashSaleStore.Application.UseCases.Catalog.Products.Queries;
using FlashSaleStore.Presentation.Abstractions;
using FlashSaleStore.Presentation.Contracts.Catalog;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlashSaleStore.Presentation.APIs.Products;

public sealed class ProductApi : ApiEndpoint, ICarterModule
{
    private const string ApiRoot = "/api/v{version:apiVersion}";
    private const string AdminProductsUrl = ApiRoot + "/admin/products";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var v1 = app.NewVersionedApi("products")
            .MapGroup(ApiRoot)
            .HasApiVersion(1);

        v1.MapGet("/home/flash-sales", GetTopFlashSaleProducts)
            .WithName("GetTopFlashSaleProducts")
            .WithTags("Home Products")
            .Produces(StatusCodes.Status200OK);

        var homeProducts = v1.MapGroup("/home/products").WithTags("Home Products");
        homeProducts.MapGet("/best-selling", GetBestSellingProducts)
            .WithName("GetBestSellingProducts")
            .Produces(StatusCodes.Status200OK);
        homeProducts.MapGet("/newest", GetNewestProducts)
            .WithName("GetNewestProducts")
            .Produces(StatusCodes.Status200OK);
        homeProducts.MapGet("/featured", GetFeaturedProducts)
            .WithName("GetFeaturedProducts")
            .Produces(StatusCodes.Status200OK);

        v1.MapGet("/flash-sales/products", GetFlashSaleProducts)
            .WithName("GetFlashSaleProducts")
            .WithTags("Products")
            .Produces(StatusCodes.Status200OK);

        v1.MapGet("/products", GetProductCatalog)
            .WithName("GetProductCatalog")
            .WithTags("Products")
            .Produces(StatusCodes.Status200OK);

        v1.MapGet("/products/{productId:guid}", GetProductDetail)
            .WithName("GetProductDetail")
            .WithTags("Products")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        var adminProducts = app.NewVersionedApi("admin-products")
            .MapGroup(AdminProductsUrl)
            .HasApiVersion(1)
            .RequireAuthorization("StoreOwner");

        adminProducts.MapGet("/", GetAdminProducts)
            .WithName("GetAdminProducts")
            .WithTags("Admin Products")
            .Produces(StatusCodes.Status200OK);
        adminProducts.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithTags("Admin Products")
            .Produces(StatusCodes.Status201Created);
        adminProducts.MapPut("/{productId:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithTags("Admin Products")
            .Produces(StatusCodes.Status200OK);
        adminProducts.MapPatch("/batch-status", BatchUpdateProductStatus)
            .WithName("BatchUpdateProductStatus")
            .WithTags("Admin Products")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetTopFlashSaleProducts(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTopFlashSaleProductsQuery(), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetBestSellingProducts(
        ISender sender,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetBestSellingProductsQuery(pageIndex, pageSize), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetNewestProducts(
        ISender sender,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetNewestProductsQuery(pageIndex, pageSize), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetFeaturedProducts(
        ISender sender,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetFeaturedProductsQuery(pageIndex, pageSize), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetFlashSaleProducts(
        ISender sender,
        Guid? categoryId = null,
        string sortByDiscount = "DESC",
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetFlashSaleProductsQuery(categoryId, sortByDiscount, pageIndex, pageSize), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetProductCatalog(
        ISender sender,
        Guid? categoryId = null,
        string sortBy = "bestSelling",
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductCatalogQuery(categoryId, sortBy, pageIndex, pageSize), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetProductDetail(Guid productId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductDetailQuery(productId), cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> GetAdminProducts(
        ISender sender,
        Guid? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? search = null,
        string? status = null,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAdminProductsQuery(categoryId, minPrice, maxPrice, search, status, pageIndex, pageSize),
            cancellationToken);
        return Handle(result);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.CategoryId,
            request.Name,
            request.Description,
            request.RegularPrice,
            request.Quantity,
            request.IsFeatured,
            request.Status);
        return Handle(await sender.Send(command, cancellationToken), StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateProduct(
        Guid productId,
        UpdateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            productId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.RegularPrice,
            request.Quantity,
            request.IsFeatured,
            request.Status);
        return Handle(await sender.Send(command, cancellationToken));
    }

    private static async Task<IResult> BatchUpdateProductStatus(
        BatchStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BatchUpdateProductStatusCommand(request.Ids, request.Status), cancellationToken);
        return Handle(result);
    }
}
