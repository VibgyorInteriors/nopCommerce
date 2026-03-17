using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Helpers;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.RequirementApi;

/// <summary>
/// Represents the Requirement API plugin
/// </summary>
public class RequirementApiPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public RequirementApiPlugin(IWebHelper webHelper)
    {
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/RequirementApi/Configure";
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        await base.UninstallAsync();
    }

    #endregion
}

