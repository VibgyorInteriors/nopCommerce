using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Vibgyor.BulkImageUpload.Controllers;

[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
public class BulkImageUploadController : BasePluginController
{
    private static readonly HashSet<string> ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff"
    ];

    private readonly INopFileProvider _fileProvider;
    private readonly IPictureService _pictureService;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;

    public BulkImageUploadController(INopFileProvider fileProvider,
        IPictureService pictureService,
        IProductService productService,
        IStoreContext storeContext)
    {
        _fileProvider = fileProvider;
        _pictureService = pictureService;
        _productService = productService;
        _storeContext = storeContext;
    }

    [CheckPermission(StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE)]
    public IActionResult Index()
    {
        return View("~/Plugins/Vibgyor.BulkImageUpload/Views/BulkImageUpload/Index.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CheckPermission(StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> Upload(IFormFile zipFile)
    {
        if (zipFile == null || zipFile.Length == 0)
            return Json(new BulkUploadResponse { Error = "Please select a ZIP file." });

        if (!string.Equals(_fileProvider.GetFileExtension(zipFile.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
            return Json(new BulkUploadResponse { Error = "Only .zip files are allowed." });

        var tempRoot = _fileProvider.Combine(_fileProvider.MapPath("~/App_Data/temp"),
            "bulk-image-upload-" + Guid.NewGuid().ToString("N"));

        _fileProvider.CreateDirectory(tempRoot);

        var extractPath = _fileProvider.Combine(tempRoot, "extract");
        var zipPath = _fileProvider.Combine(tempRoot, "upload.zip");

        try
        {
            await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await zipFile.CopyToAsync(fs);

            ExtractZipSafe(zipPath, extractPath);

            var store = await _storeContext.GetCurrentStoreAsync();
            var response = new BulkUploadResponse();

            foreach (var imagePath in EnumerateImageFiles(extractPath))
            {
                var fileName = Path.GetFileName(imagePath);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                if (string.IsNullOrWhiteSpace(nameWithoutExt))
                {
                    response.Items.Add(new BulkUploadItemResult(fileName, false, "Invalid file name."));
                    continue;
                }

                var productNameKey = nameWithoutExt.Trim();

                var search = await _productService.SearchProductsAsync(
                    pageIndex: 0,
                    pageSize: int.MaxValue,
                    storeId: store.Id,
                    keywords: productNameKey,
                    searchDescriptions: false,
                    searchManufacturerPartNumber: false,
                    searchSku: false,
                    showHidden: true);

                var product = search.FirstOrDefault(p =>
                    p.Name.Equals(productNameKey, StringComparison.OrdinalIgnoreCase));

                if (product == null)
                {
                    response.Items.Add(new BulkUploadItemResult(fileName, false, "No product found with this exact name."));
                    continue;
                }

                try
                {
                    var ext = _fileProvider.GetFileExtension(imagePath);
                    var mimeType = GetMimeTypeFromExtension(ext);
                    if (string.IsNullOrEmpty(mimeType))
                    {
                        response.Items.Add(new BulkUploadItemResult(fileName, false, "Unsupported image type."));
                        continue;
                    }

                    var pictureBinary = await _fileProvider.ReadAllBytesAsync(imagePath);
                    var picture = await _pictureService.InsertPictureAsync(pictureBinary, mimeType,
                        await _pictureService.GetPictureSeNameAsync(product.Name),
                        altAttribute: product.Name,
                        titleAttribute: product.Name);

                    var existingPictures = await _productService.GetProductPicturesByProductIdAsync(product.Id);
                    var displayOrder = existingPictures.Count == 0
                        ? 0
                        : existingPictures.Max(p => p.DisplayOrder) + 1;

                    await _productService.InsertProductPictureAsync(new ProductPicture
                    {
                        ProductId = product.Id,
                        PictureId = picture.Id,
                        DisplayOrder = displayOrder
                    });

                    await _productService.UpdateProductAsync(product);

                    response.Items.Add(new BulkUploadItemResult(fileName, true, $"Assigned to product ID {product.Id}."));
                }
                catch (Exception ex)
                {
                    response.Items.Add(new BulkUploadItemResult(fileName, false, ex.Message));
                }
            }

            response.SuccessCount = response.Items.Count(i => i.Success);
            response.FailCount = response.Items.Count(i => !i.Success);
            return Json(response);
        }
        finally
        {
            try
            {
                if (_fileProvider.DirectoryExists(tempRoot))
                    _fileProvider.DeleteDirectory(tempRoot);
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }

    private static void ExtractZipSafe(string zipPath, string extractPath)
    {
        Directory.CreateDirectory(extractPath);
        var extractFull = Path.GetFullPath(extractPath);
        if (!extractFull.EndsWith(Path.DirectorySeparatorChar))
            extractFull += Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            if (relative.EndsWith(Path.DirectorySeparatorChar))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(extractPath, relative));
            if (!destinationPath.StartsWith(extractFull, StringComparison.OrdinalIgnoreCase))
                continue;

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    private static IEnumerable<string> EnumerateImageFiles(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(path);
            if (ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                yield return path;
        }
    }

    private static string? GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => MimeTypes.ImageJpeg,
            ".png" => MimeTypes.ImagePng,
            ".gif" => MimeTypes.ImageGif,
            ".bmp" => MimeTypes.ImageBmp,
            ".webp" => MimeTypes.ImageWebp,
            ".tif" or ".tiff" => MimeTypes.ImageTiff,
            _ => null
        };
    }
}

public class BulkUploadResponse
{
    public string? Error { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<BulkUploadItemResult> Items { get; set; } = [];
}

public class BulkUploadItemResult
{
    public BulkUploadItemResult(string fileName, bool success, string message)
    {
        FileName = fileName;
        Success = success;
        Message = message;
    }

    public string FileName { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}
