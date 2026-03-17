using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.RequirementApi.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // API routes for product details
        endpointRouteBuilder.MapControllerRoute(
            name: "RequirementApi.Products",
            pattern: "api/v1/bolt/products",
            defaults: new { controller = "ProductApi", action = "GetAllProducts" });

        endpointRouteBuilder.MapControllerRoute(
            name: "RequirementApi.ProductById",
            pattern: "api/v1/bolt/products/{id}",
            defaults: new { controller = "ProductApi", action = "GetProductById" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// Higher priority means routes are registered earlier
    /// </summary>
    public int Priority => 0;
}
