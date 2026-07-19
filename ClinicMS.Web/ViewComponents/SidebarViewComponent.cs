using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly IAuthApiClient _authApiClient;

        // The real navPages catalog (modules/navPages/permissions) describes a broader app than what
        // ClinicMS.Web has screens for so far. This maps the subset of real page URLs that have an
        // actual page here onto their real MVC route -- anything not listed is left out of the menu
        // rather than linking somewhere that 404s.
        private static readonly Dictionary<string, string> RouteByPageUrl = new(StringComparer.OrdinalIgnoreCase)
        {
            ["/patients"] = "/Patients",
            ["/service-types"] = "/MedicalServices/ServiceTypes",
            ["/services"] = "/MedicalServices/Services",
            ["/treatment-plans"] = "/MedicalServices/TreatmentPlans",
            ["/patient-cycles"] = "/MedicalServices/PatientCycles",
            ["/walk-in-sale"] = "/MedicalServices/WalkInSale",

            ["/invoices"] = "/Payments/Invoices",
            ["/payments"] = "/Payments",
            ["/product-refunds"] = "/Payments/ProductRefunds",
            ["/expense-categories"] = "/Expenses/Categories",
            ["/expenses"] = "/Expenses",
            ["/recurring-expenses"] = "/Expenses/RecurringExpenses",

            ["/product-categories"] = "/SupplyChain/ProductCategories",
            ["/products"] = "/SupplyChain/Products",
            ["/product-skus"] = "/SupplyChain/ProductSkus",
            ["/stock-movements"] = "/SupplyChain/StockMovements",
            ["/suppliers"] = "/SupplyChain/Suppliers",
            ["/purchase-orders"] = "/SupplyChain/PurchaseOrders",
            ["/vendors"] = "/SupplyChain/Vendors",

            ["/settings/clinic"] = "/Settings/General",

            ["/admin/users"] = "/Users",
            ["/admin/roles"] = "/Roles",
            ["/admin/modules"] = "/Administration/Modules",
            ["/admin/nav-pages"] = "/Administration/NavPages",
            ["/admin/report-pages"] = "/Administration/ReportPages",
            ["/admin/audit"] = "/Activity",
        };

        // Keyed on the module names actually seeded on the live database (Registration, Finance,
        // Supply Chain, Configuration, Administration, Reports) -- confirmed directly against the
        // Modules table rather than DataSeeder.cs's current source, since this DB was seeded before
        // that source's module names were last changed and never re-seeded.
        private static readonly Dictionary<string, string> IconByModuleName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Registration"] = "ri-file-list-3-line",
            ["Finance"] = "ri-bank-card-line",
            ["Supply Chain"] = "ri-truck-line",
            ["Configuration"] = "ri-settings-3-line",
            ["Administration"] = "ri-shield-user-line",
            ["Reports"] = "ri-bar-chart-box-line",
        };

        public SidebarViewComponent(IAuthApiClient authApiClient)
        {
            _authApiClient = authApiClient;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var modules = new List<SidebarModule>();

            try
            {
                var menu = await _authApiClient.GetMenuAsync();
                foreach (var module in menu.Modules)
                {
                    var items = module.Pages
                        .Select(BuildNavItem)
                        .Where(item => item is not null)
                        .Select(item => item!)
                        .ToList();

                    if (items.Count > 0)
                    {
                        var icon = IconByModuleName.GetValueOrDefault(module.ModuleName, "ri-folder-3-line");
                        modules.Add(new SidebarModule(module.ModuleName, icon, items));
                    }
                }
            }
            catch (ApiException)
            {
                // Session expired / API unreachable -- render with just the fixed items below rather
                // than taking the whole page down; RequireAuth on the actual page will redirect if needed.
            }

            return View(modules);
        }

        private static SidebarNavItem? BuildNavItem(MenuPageDto page)
        {
            if (page.SubPages is { Count: > 0 })
            {
                var children = page.SubPages
                    .Select(BuildNavItem)
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .ToList();

                return children.Count > 0 ? new SidebarNavItem(page.PageName, null, children) : null;
            }

            if (!page.CanView || !RouteByPageUrl.TryGetValue(page.PageUrl, out var route))
            {
                return null;
            }

            return new SidebarNavItem(page.PageName, route, Array.Empty<SidebarNavItem>());
        }
    }
}
