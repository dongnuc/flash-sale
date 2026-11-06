using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Movie_StructureCode.Presentation.Abstractions;

namespace Movie_StructureCode.Presentation.APIs.Products
{
    public class ProductApi : ApiEndpoint, ICarterModule
    {

        private const string BaseUrl = "/api/v{version:apiVersion}/products";
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group1 = app.NewVersionedApi("products")
                .MapGroup(BaseUrl).HasApiVersion(1);

            var group2 = app.NewVersionedApi("products")
               .MapGroup(BaseUrl).HasApiVersion(2);

            group1.MapPost("/", () => "");
            group1.MapGet("/", () => "");

            group2.MapGet("/", () => "");

        }
    }
}
