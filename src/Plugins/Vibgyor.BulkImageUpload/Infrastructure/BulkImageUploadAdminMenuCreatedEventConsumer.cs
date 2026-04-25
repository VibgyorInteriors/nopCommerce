using Nop.Core;
using Nop.Services.Events;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Vibgyor.BulkImageUpload.Infrastructure;

/// <summary>
/// Adds the Catalog menu item for admins who do not have "Manage plugins" (IAdminMenuPlugin runs only for that group).
/// </summary>
public class BulkImageUploadAdminMenuCreatedEventConsumer : IConsumer<AdminMenuCreatedEvent>
{
    private readonly IPluginManager<IPlugin> _pluginManager;
    private readonly IPermissionService _permissionService;
    private readonly IWorkContext _workContext;

    public BulkImageUploadAdminMenuCreatedEventConsumer(IPluginManager<IPlugin> pluginManager,
        IPermissionService permissionService,
        IWorkContext workContext)
    {
        _pluginManager = pluginManager;
        _permissionService = permissionService;
        _workContext = workContext;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        var plugin = await _pluginManager.LoadPluginBySystemNameAsync(BulkImageUploadDefaults.SystemName);
        if (plugin == null)
            return;

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS, customer))
            return;

        eventMessage.RootMenuItem.InsertAfter("Products", new AdminMenuItem
        {
            SystemName = BulkImageUploadDefaults.SystemName,
            Title = "Bulk Image Upload",
            Url = eventMessage.GetMenuItemUrl("BulkImageUpload", "Index"),
            IconClass = "far fa-images",
            PermissionNames = [StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE]
        });
    }
}
