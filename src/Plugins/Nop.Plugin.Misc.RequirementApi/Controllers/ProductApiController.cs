using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Media;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Misc.RequirementApi.Controllers;

/// <summary>
/// RESTful API controller for product details
/// </summary>
[ApiController]
[Route("api/v1/bolt/products")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class ProductApiController : BasePluginController
{
    #region Fields

    protected readonly IProductService _productService;
    protected readonly IPictureService _pictureService;
    protected readonly ICategoryService _categoryService;
    protected readonly IManufacturerService _manufacturerService;
    protected readonly IDiscountService _discountService;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly ISpecificationAttributeService _specificationAttributeService;
    protected readonly IProductTagService _productTagService;
    protected readonly IWorkContext _workContext;
    protected readonly ICustomerService _customerService;

    #endregion

    #region Ctor

    public ProductApiController(
        IProductService productService,
        IPictureService pictureService,
        ICategoryService categoryService,
        IManufacturerService manufacturerService,
        IDiscountService discountService,
        IProductAttributeService productAttributeService,
        ISpecificationAttributeService specificationAttributeService,
        IProductTagService productTagService,
        IWorkContext workContext,
        ICustomerService customerService)
    {
        _productService = productService;
        _pictureService = pictureService;
        _categoryService = categoryService;
        _manufacturerService = manufacturerService;
        _discountService = discountService;
        _productAttributeService = productAttributeService;
        _specificationAttributeService = specificationAttributeService;
        _productTagService = productTagService;
        _workContext = workContext;
        _customerService = customerService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get all products with full details
    /// GET /api/v1/bolt/products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> GetAllProducts(
        [FromQuery] int pageIndex = 0, 
        [FromQuery] int pageSize = int.MaxValue,
        [FromQuery] bool showHidden = false)
    {
        var products = await _productService.SearchProductsAsync(
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: showHidden);

        var result = new List<object>();

        foreach (var product in products)
        {
            // Get product pictures
            var productPictures = await _productService.GetProductPicturesByProductIdAsync(product.Id);
            var pictureUrls = new List<string>();
            
            foreach (var productPicture in productPictures)
            {
                var pictureUrl = await _pictureService.GetPictureUrlAsync(productPicture.PictureId);
                if (!string.IsNullOrEmpty(pictureUrl))
                {
                    pictureUrls.Add(pictureUrl);
                }
            }

            // Get product categories
            var productCategories = await _categoryService.GetProductCategoriesByProductIdAsync(product.Id);
            var categoryIds = productCategories.Select(pc => pc.CategoryId).ToList();
            var categoryList = new List<object>();
            foreach (var productCategory in productCategories)
            {
                var category = await _categoryService.GetCategoryByIdAsync(productCategory.CategoryId);
                if (category != null)
                {
                    categoryList.Add(new
                    {
                        id = category.Id,
                        name = category.Name
                    });
                }
            }

            // Get product manufacturers
            var productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdAsync(product.Id);
            var manufacturerIds = productManufacturers.Select(pm => pm.ManufacturerId).ToList();

            // Get product tags
            var productTags = await _productTagService.GetAllProductTagsByProductIdAsync(product.Id);
            var productTagNames = productTags.Select(pt => pt.Name).ToList();

            // Get related products
            var relatedProducts = await _productService.GetRelatedProductsByProductId1Async(product.Id, showHidden);
            var relatedProductIds = relatedProducts.Select(rp => rp.ProductId2).ToList();

            // Get cross-sell products
            var crossSellProducts = await _productService.GetCrossSellProductsByProductId1Async(product.Id, showHidden);
            var crossSellProductIds = crossSellProducts.Select(cs => cs.ProductId2).ToList();

            // Get product attributes
            var productAttributes = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
            var productAttributeList = new List<object>();
            foreach (var attribute in productAttributes)
            {
                var attributeValues = await _productAttributeService.GetProductAttributeValuesAsync(attribute.Id);
                productAttributeList.Add(new
                {
                    id = attribute.Id,
                    productAttributeId = attribute.ProductAttributeId,
                    productAttributeName = (await _productAttributeService.GetProductAttributeByIdAsync(attribute.ProductAttributeId))?.Name,
                    textPrompt = attribute.TextPrompt,
                    isRequired = attribute.IsRequired,
                    attributeControlTypeId = attribute.AttributeControlTypeId,
                    displayOrder = attribute.DisplayOrder,
                    values = attributeValues.Select(av => new
                    {
                        id = av.Id,
                        name = av.Name,
                        priceAdjustment = av.PriceAdjustment,
                        weightAdjustment = av.WeightAdjustment,
                        isPreSelected = av.IsPreSelected,
                        displayOrder = av.DisplayOrder
                    }).ToList()
                });
            }

            // Get specification attributes
            var specificationAttributes = await _specificationAttributeService.GetProductSpecificationAttributesAsync(product.Id);
            var specificationAttributeList = specificationAttributes.Select(sa => new
            {
                id = sa.Id,
                specificationAttributeOptionId = sa.SpecificationAttributeOptionId,
                allowFiltering = sa.AllowFiltering,
                showOnProductPage = sa.ShowOnProductPage,
                displayOrder = sa.DisplayOrder
            }).ToList();

            // Get tier prices
            var tierPrices = await _productService.GetTierPricesByProductAsync(product.Id);
            var tierPriceList = tierPrices.Select(tp => new
            {
                id = tp.Id,
                storeId = tp.StoreId,
                customerRoleId = tp.CustomerRoleId,
                quantity = tp.Quantity,
                price = tp.Price,
                startDateTimeUtc = tp.StartDateTimeUtc,
                endDateTimeUtc = tp.EndDateTimeUtc
            }).ToList();

            // Get discounts applied to product and categories
            var discountMappings = await _productService.GetAllDiscountsAppliedToProductAsync(product.Id);
            var discountList = new List<object>();
            var processedDiscountIds = new HashSet<int>(); // Track processed discounts to avoid duplicates
            decimal totalDiscountAmount = 0;
            decimal? discountPercentage = null;
            var nowUtc = DateTime.UtcNow;
            var customer = await _workContext.GetCurrentCustomerAsync();

            // Get product-level discounts
            foreach (var discountMapping in discountMappings)
            {
                var discount = await _discountService.GetDiscountByIdAsync(discountMapping.DiscountId);
                if (discount != null && discount.IsActive && !processedDiscountIds.Contains(discount.Id))
                {
                    // Check if discount is valid based on date range
                    if (discount.StartDateUtc.HasValue && discount.StartDateUtc.Value > nowUtc)
                        continue;
                    if (discount.EndDateUtc.HasValue && discount.EndDateUtc.Value < nowUtc)
                        continue;

                    // Calculate discount amount using discount service
                    var discountAmount = _discountService.GetDiscountAmount(discount, product.Price);

                    totalDiscountAmount += discountAmount;

                    // Get discount percentage (if percentage-based) or calculate percentage from amount
                    decimal? percentage = null;
                    if (discount.UsePercentage)
                    {
                        percentage = discount.DiscountPercentage;
                    }
                    else if (product.Price > 0)
                    {
                        // Calculate percentage from fixed amount
                        percentage = (discountAmount / product.Price) * 100;
                    }

                    // Use the highest percentage if multiple discounts
                    if (!discountPercentage.HasValue || (percentage.HasValue && percentage.Value > discountPercentage.Value))
                    {
                        discountPercentage = percentage;
                    }

                    discountList.Add(new
                    {
                        id = discount.Id,
                        name = discount.Name,
                        discountType = discount.DiscountType.ToString(),
                        appliedTo = "product",
                        discountPercentage = percentage,
                        discountAmount = discountAmount,
                        usePercentage = discount.UsePercentage
                    });

                    processedDiscountIds.Add(discount.Id);
                }
            }

            // Get category-level discounts
            // Use the categoryIds we already have from earlier in the method
            var productCategoryIdsForDiscounts = categoryIds;

            // Get all discounts assigned to categories
            var categoryDiscounts = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToCategories);
            
            foreach (var discount in categoryDiscounts)
            {
                if (!discount.IsActive || processedDiscountIds.Contains(discount.Id))
                    continue;

                // Check if discount is valid based on date range
                if (discount.StartDateUtc.HasValue && discount.StartDateUtc.Value > nowUtc)
                    continue;
                if (discount.EndDateUtc.HasValue && discount.EndDateUtc.Value < nowUtc)
                    continue;

                // Get category IDs this discount is applied to
                var discountCategoryIds = await _categoryService.GetAppliedCategoryIdsAsync(discount, customer);

            // Check if any of the product's categories match the discount categories
            var matchingCategoryIds = productCategoryIdsForDiscounts.Intersect(discountCategoryIds).ToList();
                if (matchingCategoryIds.Any())
                {
                    // Validate discount (check coupon codes, etc.)
                    var couponCodesToValidate = await _customerService.ParseAppliedDiscountCouponCodesAsync(customer);
                    var validationResult = await _discountService.ValidateDiscountAsync(discount, customer, couponCodesToValidate);
                    
                    if (validationResult.IsValid)
                    {
                        // Calculate discount amount using discount service
                        var discountAmount = _discountService.GetDiscountAmount(discount, product.Price);

                        totalDiscountAmount += discountAmount;

                        // Get discount percentage (if percentage-based) or calculate percentage from amount
                        decimal? percentage = null;
                        if (discount.UsePercentage)
                        {
                            percentage = discount.DiscountPercentage;
                        }
                        else if (product.Price > 0)
                        {
                            // Calculate percentage from fixed amount
                            percentage = (discountAmount / product.Price) * 100;
                        }

                        // Use the highest percentage if multiple discounts
                        if (!discountPercentage.HasValue || (percentage.HasValue && percentage.Value > discountPercentage.Value))
                        {
                            discountPercentage = percentage;
                        }

                        discountList.Add(new
                        {
                            id = discount.Id,
                            name = discount.Name,
                            discountType = discount.DiscountType.ToString(),
                            appliedTo = "category",
                            categoryIds = matchingCategoryIds,
                            discountPercentage = percentage,
                            discountAmount = discountAmount,
                            usePercentage = discount.UsePercentage
                        });

                        processedDiscountIds.Add(discount.Id);
                    }
                }
            }

            result.Add(new
            {
                id = product.Id,
                name = product.Name,
                shortDescription = product.ShortDescription,
                fullDescription = product.FullDescription,
                sku = product.Sku,
                gtin = product.Gtin,
                manufacturerPartNumber = product.ManufacturerPartNumber,
                price = product.Price,
                oldPrice = product.OldPrice,
                productCost = product.ProductCost,
                weight = product.Weight,
                length = product.Length,
                width = product.Width,
                height = product.Height,
                published = product.Published,
                deleted = product.Deleted,
                visibleIndividually = product.VisibleIndividually,
                stockQuantity = product.StockQuantity,
                manageInventoryMethod = product.ManageInventoryMethod.ToString(),
                displayStockAvailability = product.DisplayStockAvailability,
                displayStockQuantity = product.DisplayStockQuantity,
                displayOrder = product.DisplayOrder,
                createdOnUtc = product.CreatedOnUtc,
                updatedOnUtc = product.UpdatedOnUtc,
                productType = ((ProductType)product.ProductTypeId).ToString(),
                vendorId = product.VendorId,
                showOnHomepage = product.ShowOnHomepage,
                allowCustomerReviews = product.AllowCustomerReviews,
                approvedRatingSum = product.ApprovedRatingSum,
                notApprovedRatingSum = product.NotApprovedRatingSum,
                approvedTotalReviews = product.ApprovedTotalReviews,
                notApprovedTotalReviews = product.NotApprovedTotalReviews,
                availableStartDateTimeUtc = product.AvailableStartDateTimeUtc,
                availableEndDateTimeUtc = product.AvailableEndDateTimeUtc,
                markAsNew = product.MarkAsNew,
                markAsNewStartDateTimeUtc = product.MarkAsNewStartDateTimeUtc,
                markAsNewEndDateTimeUtc = product.MarkAsNewEndDateTimeUtc,
                pictures = pictureUrls,
                categoryIds = categoryIds,
                categories = categoryList,
                manufacturerIds = manufacturerIds,
                productTags = productTagNames,
                relatedProductIds = relatedProductIds,
                crossSellProductIds = crossSellProductIds,
                productAttributes = productAttributeList,
                specificationAttributes = specificationAttributeList,
                tierPrices = tierPriceList,
                discounts = discountList,
                discountPercentage = discountPercentage,
                totalDiscountAmount = totalDiscountAmount,
                finalPrice = product.Price - totalDiscountAmount
            });
        }

        return Ok(new
        {
            success = true,
            data = result,
            totalCount = products.TotalCount,
            pageIndex = pageIndex,
            pageSize = pageSize
        });
    }

    /// <summary>
    /// Get product by ID with full details
    /// GET /api/v1/bolt/products/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> GetProductById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        
        if (product == null || product.Deleted)
        {
            return NotFound(new
            {
                success = false,
                message = "Product not found"
            });
        }

        // Get product pictures
        var productPictures = await _productService.GetProductPicturesByProductIdAsync(product.Id);
        var pictureUrls = new List<string>();
        
        foreach (var productPicture in productPictures)
        {
            var pictureUrl = await _pictureService.GetPictureUrlAsync(productPicture.PictureId);
            if (!string.IsNullOrEmpty(pictureUrl))
            {
                pictureUrls.Add(pictureUrl);
            }
        }

        // Get product categories
        var productCategories = await _categoryService.GetProductCategoriesByProductIdAsync(product.Id);
        var categoryIds = productCategories.Select(pc => pc.CategoryId).ToList();
        var categoryList = new List<object>();
        foreach (var productCategory in productCategories)
        {
            var category = await _categoryService.GetCategoryByIdAsync(productCategory.CategoryId);
            if (category != null)
            {
                categoryList.Add(new
                {
                    id = category.Id,
                    name = category.Name
                });
            }
        }

        // Get product manufacturers
        var productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdAsync(product.Id);
        var manufacturerIds = productManufacturers.Select(pm => pm.ManufacturerId).ToList();

        // Get discounts applied to product and categories
        var discountMappings = await _productService.GetAllDiscountsAppliedToProductAsync(product.Id);
        var discountList = new List<object>();
        var processedDiscountIds = new HashSet<int>(); // Track processed discounts to avoid duplicates
        decimal totalDiscountAmount = 0;
        decimal? discountPercentage = null;
        var nowUtc = DateTime.UtcNow;
        var customer = await _workContext.GetCurrentCustomerAsync();

        // Get product-level discounts
        foreach (var discountMapping in discountMappings)
        {
            var discount = await _discountService.GetDiscountByIdAsync(discountMapping.DiscountId);
            if (discount != null && discount.IsActive && !processedDiscountIds.Contains(discount.Id))
            {
                // Check if discount is valid based on date range
                if (discount.StartDateUtc.HasValue && discount.StartDateUtc.Value > nowUtc)
                    continue;
                if (discount.EndDateUtc.HasValue && discount.EndDateUtc.Value < nowUtc)
                    continue;

                // Calculate discount amount using discount service
                var discountAmount = _discountService.GetDiscountAmount(discount, product.Price);

                totalDiscountAmount += discountAmount;

                // Get discount percentage (if percentage-based) or calculate percentage from amount
                decimal? percentage = null;
                if (discount.UsePercentage)
                {
                    percentage = discount.DiscountPercentage;
                }
                else if (product.Price > 0)
                {
                    // Calculate percentage from fixed amount
                    percentage = (discountAmount / product.Price) * 100;
                }

                // Use the highest percentage if multiple discounts
                if (!discountPercentage.HasValue || (percentage.HasValue && percentage.Value > discountPercentage.Value))
                {
                    discountPercentage = percentage;
                }

                discountList.Add(new
                {
                    id = discount.Id,
                    name = discount.Name,
                    discountType = discount.DiscountType.ToString(),
                    appliedTo = "product",
                    discountPercentage = percentage,
                    discountAmount = discountAmount,
                    usePercentage = discount.UsePercentage
                });

                processedDiscountIds.Add(discount.Id);
            }
        }

        // Get category-level discounts
        var productCategoryIdsForDiscounts = categoryIds; // Use the categoryIds we already have

        // Get all discounts assigned to categories
        var categoryDiscounts = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToCategories);
        
        foreach (var discount in categoryDiscounts)
        {
            if (!discount.IsActive || processedDiscountIds.Contains(discount.Id))
                continue;

            // Check if discount is valid based on date range
            if (discount.StartDateUtc.HasValue && discount.StartDateUtc.Value > nowUtc)
                continue;
            if (discount.EndDateUtc.HasValue && discount.EndDateUtc.Value < nowUtc)
                continue;

            // Get category IDs this discount is applied to
            var discountCategoryIds = await _categoryService.GetAppliedCategoryIdsAsync(discount, customer);

            // Check if any of the product's categories match the discount categories
            var matchingCategoryIds = productCategoryIdsForDiscounts.Intersect(discountCategoryIds).ToList();
            if (matchingCategoryIds.Any())
            {
                // Validate discount (check coupon codes, etc.)
                var couponCodesToValidate = await _customerService.ParseAppliedDiscountCouponCodesAsync(customer);
                var validationResult = await _discountService.ValidateDiscountAsync(discount, customer, couponCodesToValidate);
                
                if (validationResult.IsValid)
                {
                    // Calculate discount amount using discount service
                    var discountAmount = _discountService.GetDiscountAmount(discount, product.Price);

                    totalDiscountAmount += discountAmount;

                    // Get discount percentage (if percentage-based) or calculate percentage from amount
                    decimal? percentage = null;
                    if (discount.UsePercentage)
                    {
                        percentage = discount.DiscountPercentage;
                    }
                    else if (product.Price > 0)
                    {
                        // Calculate percentage from fixed amount
                        percentage = (discountAmount / product.Price) * 100;
                    }

                    // Use the highest percentage if multiple discounts
                    if (!discountPercentage.HasValue || (percentage.HasValue && percentage.Value > discountPercentage.Value))
                    {
                        discountPercentage = percentage;
                    }

                    discountList.Add(new
                    {
                        id = discount.Id,
                        name = discount.Name,
                        discountType = discount.DiscountType.ToString(),
                        appliedTo = "category",
                        categoryIds = matchingCategoryIds,
                        discountPercentage = percentage,
                        discountAmount = discountAmount,
                        usePercentage = discount.UsePercentage
                    });

                    processedDiscountIds.Add(discount.Id);
                }
            }
        }

        var result = new
        {
            id = product.Id,
            name = product.Name,
            shortDescription = product.ShortDescription,
            fullDescription = product.FullDescription,
            adminComment = product.AdminComment,
            sku = product.Sku,
            gtin = product.Gtin,
            manufacturerPartNumber = product.ManufacturerPartNumber,
            price = product.Price,
            oldPrice = product.OldPrice,
            productCost = product.ProductCost,
            weight = product.Weight,
            length = product.Length,
            width = product.Width,
            height = product.Height,
            published = product.Published,
            deleted = product.Deleted,
            visibleIndividually = product.VisibleIndividually,
            stockQuantity = product.StockQuantity,
            manageInventoryMethod = product.ManageInventoryMethod.ToString(),
            displayStockAvailability = product.DisplayStockAvailability,
            displayStockQuantity = product.DisplayStockQuantity,
            displayOrder = product.DisplayOrder,
            createdOnUtc = product.CreatedOnUtc,
            updatedOnUtc = product.UpdatedOnUtc,
            productType = ((ProductType)product.ProductTypeId).ToString(),
            productTypeId = product.ProductTypeId,
            parentGroupedProductId = product.ParentGroupedProductId,
            vendorId = product.VendorId,
            showOnHomepage = product.ShowOnHomepage,
            allowCustomerReviews = product.AllowCustomerReviews,
            approvedRatingSum = product.ApprovedRatingSum,
            notApprovedRatingSum = product.NotApprovedRatingSum,
            approvedTotalReviews = product.ApprovedTotalReviews,
            notApprovedTotalReviews = product.NotApprovedTotalReviews,
            availableStartDateTimeUtc = product.AvailableStartDateTimeUtc,
            availableEndDateTimeUtc = product.AvailableEndDateTimeUtc,
            markAsNew = product.MarkAsNew,
            markAsNewStartDateTimeUtc = product.MarkAsNewStartDateTimeUtc,
            markAsNewEndDateTimeUtc = product.MarkAsNewEndDateTimeUtc,
            pictures = pictureUrls,
            categoryIds = categoryIds,
            categories = categoryList,
            manufacturerIds = manufacturerIds,
            productTemplateId = product.ProductTemplateId,
            warehouseId = product.WarehouseId,
            useMultipleWarehouses = product.UseMultipleWarehouses,
            minStockQuantity = product.MinStockQuantity,
            lowStockActivity = product.LowStockActivity.ToString(),
            notifyAdminForQuantityBelow = product.NotifyAdminForQuantityBelow,
            backorderMode = product.BackorderMode.ToString(),
            allowBackInStockSubscriptions = product.AllowBackInStockSubscriptions,
            orderMinimumQuantity = product.OrderMinimumQuantity,
            orderMaximumQuantity = product.OrderMaximumQuantity,
            allowedQuantities = product.AllowedQuantities,
            disableBuyButton = product.DisableBuyButton,
            disableWishlistButton = product.DisableWishlistButton,
            availableForPreOrder = product.AvailableForPreOrder,
            preOrderAvailabilityStartDateTimeUtc = product.PreOrderAvailabilityStartDateTimeUtc,
            callForPrice = product.CallForPrice,
            basepriceEnabled = product.BasepriceEnabled,
            basepriceAmount = product.BasepriceAmount,
            basepriceUnitId = product.BasepriceUnitId,
            basepriceBaseAmount = product.BasepriceBaseAmount,
            basepriceBaseUnitId = product.BasepriceBaseUnitId,
            isShipEnabled = product.IsShipEnabled,
            isFreeShipping = product.IsFreeShipping,
            shipSeparately = product.ShipSeparately,
            additionalShippingCharge = product.AdditionalShippingCharge,
            deliveryDateId = product.DeliveryDateId,
            isTaxExempt = product.IsTaxExempt,
            taxCategoryId = product.TaxCategoryId,
            downloadId = product.DownloadId,
            unlimitedDownloads = product.UnlimitedDownloads,
            maxNumberOfDownloads = product.MaxNumberOfDownloads,
            downloadExpirationDays = product.DownloadExpirationDays,
            downloadActivationType = product.DownloadActivationType.ToString(),
            hasSampleDownload = product.HasSampleDownload,
            sampleDownloadId = product.SampleDownloadId,
            hasUserAgreement = product.HasUserAgreement,
            userAgreementText = product.UserAgreementText,
            isRecurring = product.IsRecurring,
            recurringCycleLength = product.RecurringCycleLength,
            recurringCyclePeriod = product.RecurringCyclePeriod.ToString(),
            recurringTotalCycles = product.RecurringTotalCycles,
            isRental = product.IsRental,
            rentalPriceLength = product.RentalPriceLength,
            rentalPricePeriod = product.RentalPricePeriod.ToString(),
            requireOtherProducts = product.RequireOtherProducts,
            requiredProductIds = product.RequiredProductIds,
            automaticallyAddRequiredProducts = product.AutomaticallyAddRequiredProducts,
            giftCardType = product.GiftCardType.ToString(),
            discounts = discountList,
            discountPercentage = discountPercentage,
            totalDiscountAmount = totalDiscountAmount,
            finalPrice = product.Price - totalDiscountAmount
        };

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    #endregion
}
