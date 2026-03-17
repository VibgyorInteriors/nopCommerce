using Nop.Core;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.QuotationApi;

/// <summary>
/// Represents the Quotation API plugin
/// </summary>
public class QuotationApiPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public QuotationApiPlugin(IWebHelper webHelper)
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
        return $"{_webHelper.GetStoreLocation()}Admin/QuotationApi/Configure";
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

