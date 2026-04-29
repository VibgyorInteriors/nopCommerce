using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.QuotationApi.Controllers;

/// <summary>
/// RESTful API controller for Angular quotation application
/// </summary>
[Route("api/quotation")]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public class QuotationApiController : BasePluginController
{
    /// <summary>Default batch size when the client omits <c>pageSize</c> (avoid loading the entire catalog at once).</summary>
    private const int DefaultProductPageSize = 50;

    /// <summary>Upper bound per request to keep payloads and DB work predictable.</summary>
    private const int MaxProductPageSize = 500;

    #region Fields

    protected readonly IProductService _productService;
    protected readonly IDiscountService _discountService;
    protected readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public QuotationApiController(
        IProductService productService,
        IDiscountService discountService,
        IPermissionService permissionService)
    {
        _productService = productService;
        _discountService = discountService;
        _permissionService = permissionService;
    }

    #endregion

    #region Products

    /// <summary>
    /// Get products in batches for quotation selection. Call repeatedly with increasing <paramref name="pageIndex"/> until <c>hasNextPage</c> is false.
    /// GET /api/quotation/products?pageIndex=0&amp;pageSize=50
    /// </summary>
    [HttpGet("products")]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_VIEW)]
    public virtual async Task<IActionResult> GetProducts([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = DefaultProductPageSize)
    {
        if (pageIndex < 0)
            pageIndex = 0;
        if (pageSize < 1)
            pageSize = DefaultProductPageSize;
        else if (pageSize > MaxProductPageSize)
            pageSize = MaxProductPageSize;

        var products = await _productService.SearchProductsAsync(
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: true);

        var result = products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            sku = p.Sku,
            price = p.Price,
            oldPrice = p.OldPrice,
            productCost = p.ProductCost,
            published = p.Published,
            stockQuantity = p.StockQuantity,
            manageInventoryMethod = p.ManageInventoryMethod.ToString()
        }).ToList();

        var totalCount = products.TotalCount;
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        var loadedThroughCount = Math.Min(pageIndex * pageSize + result.Count, totalCount);

        return Json(new
        {
            data = result,
            totalCount,
            pageIndex,
            pageSize,
            totalPages,
            batchIndex = pageIndex,
            batchNumber = totalPages == 0 ? 0 : pageIndex + 1,
            batchCount = totalPages,
            batchItemCount = result.Count,
            hasNextPage = totalPages > 0 && pageIndex + 1 < totalPages,
            hasPreviousPage = pageIndex > 0 && totalCount > 0,
            loadedThroughCount,
            progressPercent = totalCount == 0 ? 100m : Math.Round(100m * loadedThroughCount / totalCount, 2)
        });
    }

    /// <summary>
    /// Get product by ID
    /// GET /api/quotation/products/{id}
    /// </summary>
    [HttpGet("products/{id}")]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_VIEW)]
    public virtual async Task<IActionResult> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        return Json(new
        {
            id = product.Id,
            name = product.Name,
            sku = product.Sku,
            price = product.Price,
            oldPrice = product.OldPrice,
            productCost = product.ProductCost,
            published = product.Published,
            stockQuantity = product.StockQuantity,
            manageInventoryMethod = product.ManageInventoryMethod.ToString(),
            shortDescription = product.ShortDescription,
            fullDescription = product.FullDescription
        });
    }

    #endregion

    #region Discounts

    /// <summary>
    /// Get all discounts
    /// GET /api/quotation/discounts
    /// </summary>
    [HttpGet("discounts")]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_VIEW)]
    public virtual async Task<IActionResult> GetDiscounts()
    {
        var discounts = await _discountService.GetAllDiscountsAsync(
            discountType: null,
            couponCode: null,
            discountName: null,
            showHidden: true);

        var result = discounts.Select(d => new
        {
            id = d.Id,
            name = d.Name,
            discountType = d.DiscountType.ToString(),
            usePercentage = d.UsePercentage,
            discountPercentage = d.DiscountPercentage,
            discountAmount = d.DiscountAmount,
            startDateUtc = d.StartDateUtc,
            endDateUtc = d.EndDateUtc,
            requiresCouponCode = d.RequiresCouponCode,
            couponCode = d.CouponCode,
            isActive = d.IsActive
        }).ToList();

        return Json(new { data = result });
    }

    #endregion

    #region Quotations

    /// <summary>
    /// Get all quotations
    /// GET /api/quotation/quotations
    /// </summary>
    [HttpGet("quotations")]
    public virtual IActionResult GetQuotations()
    {
        // TODO: Implement quotation storage/retrieval
        // This is a placeholder - you'll need to create a quotation service
        return Json(new { data = new List<object>(), message = "Quotation feature coming soon" });
    }

    /// <summary>
    /// Create a new quotation
    /// POST /api/quotation/quotations
    /// </summary>
    [HttpPost("quotations")]
    public virtual async Task<IActionResult> CreateQuotation([FromBody] CreateQuotationRequest request)
    {
        // TODO: Implement quotation creation
        // This is a placeholder - you'll need to create a quotation service
        return Json(new { 
            success = true, 
            message = "Quotation created successfully",
            quotationId = 1 // Placeholder
        });
    }

    #endregion
}

/// <summary>
/// Request model for creating a quotation
/// </summary>
public class CreateQuotationRequest
{
    public int CustomerId { get; set; }
    public List<QuotationItemRequest> Items { get; set; } = new();
    public int? DiscountId { get; set; }
    public string Notes { get; set; }
}

/// <summary>
/// Quotation item request model
/// </summary>
public class QuotationItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

