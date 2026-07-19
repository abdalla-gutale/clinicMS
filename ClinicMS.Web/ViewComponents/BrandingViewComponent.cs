using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.ViewComponents
{
    /// <summary>Renders the browser-tab favicon from the clinic's Icon Logo, falling back to the
    /// static bundled icon when unset or the settings call fails.</summary>
    public class BrandingViewComponent : ViewComponent
    {
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IConfiguration _configuration;

        public BrandingViewComponent(ISettingsApiClient settingsApiClient, IConfiguration configuration)
        {
            _settingsApiClient = settingsApiClient;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string? iconUrl = null;
            try
            {
                var settings = await _settingsApiClient.GetClinicSettingsAsync();
                iconUrl = LogoUrlResolver.Resolve(settings?.LogoIconUrl, _configuration);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
            {
                // API unreachable / not configured yet -- fall back to the static default below.
            }

            // Explicit view name required: View(string) would otherwise be read as a view-name
            // overload rather than "model", since the model type here is itself a string.
            return View("Default", iconUrl ?? "/assets/images/logo-sm.png");
        }
    }
}
