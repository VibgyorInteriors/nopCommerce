#pragma warning disable CS0618 // IAdminMenuPlugin: required by spec; prefer AdminMenuCreatedEvent for new code
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;

namespace Vibgyor.BulkImageUpload;

/// <summary>Registers the Bulk Image Upload plugin and admin menu (for users with Manage plugins permission).</summary>
public class BulkImageUploadPlugin : BasePlugin, IAdminMenuPlugin
{
    /// <inheritdoc />
    public Task ManageSiteMapAsync(AdminMenuItem rootNode)
    {
        var catalog = rootNode.GetItemBySystemName("Catalog");
        catalog?.InsertAfter("Products", new AdminMenuItem
        {
            SystemName = BulkImageUploadDefaults.SystemName,
            Title = "Bulk Image Upload",
            Url = "Admin/BulkImageUpload/Index",
            IconClass = "far fa-images",
            PermissionNames = [StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE]
        });

        return Task.CompletedTask;
    }
}

#pragma warning restore CS0618
