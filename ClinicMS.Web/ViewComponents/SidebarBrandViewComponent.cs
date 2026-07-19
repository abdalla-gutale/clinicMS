using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.ViewComponents
{
    /// <summary>Renders the sidebar brand box from the Sidebar Logo alone (falls back to a static
    /// icon + clinic name wordmark when unset), used for both the expanded and collapsed states.</summary>
    public class SidebarBrandViewComponent : ViewComponent
    {
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IConfiguration _configuration;

        public SidebarBrandViewComponent(ISettingsApiClient settingsApiClient, IConfiguration configuration)
        {
            _settingsApiClient = settingsApiClient;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            ClinicSettingDto? settings = null;
            try
            {
                settings = await _settingsApiClient.GetClinicSettingsAsync();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
            {
                // API unreachable / not configured yet -- render with the static text fallback below.
            }

            var model = new SidebarBrandModel(
                settings?.ClinicName ?? "ClinicMS",
                LogoUrlResolver.Resolve(settings?.SidebarLogoUrl, _configuration));

            return View(model);
        }
    }

    public record SidebarBrandModel(string ClinicName, string? SidebarLogoUrl);
}
