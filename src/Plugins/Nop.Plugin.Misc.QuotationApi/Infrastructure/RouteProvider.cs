using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.QuotationApi.Infrastructure;

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
        // API routes for Angular consumption
        endpointRouteBuilder.MapControllerRoute(
            name: "QuotationApi.Products",
            pattern: "api/quotation/products",
            defaults: new { controller = "QuotationApi", action = "GetProducts" });

        endpointRouteBuilder.MapControllerRoute(
            name: "QuotationApi.ProductById",
            pattern: "api/quotation/products/{id}",
            defaults: new { controller = "QuotationApi", action = "GetProduct" });

        endpointRouteBuilder.MapControllerRoute(
            name: "QuotationApi.Discounts",
            pattern: "api/quotation/discounts",
            defaults: new { controller = "QuotationApi", action = "GetDiscounts" });

        endpointRouteBuilder.MapControllerRoute(
            name: "QuotationApi.Quotations",
            pattern: "api/quotation/quotations",
            defaults: new { controller = "QuotationApi", action = "GetQuotations" });

        endpointRouteBuilder.MapControllerRoute(
            name: "QuotationApi.CreateQuotation",
            pattern: "api/quotation/quotations",
            defaults: new { controller = "QuotationApi", action = "CreateQuotation" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}

