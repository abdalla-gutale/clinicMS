using Microsoft.AspNetCore.Mvc;
namespace ClinicMS.Web.Controllers {
    public class MembershipController : Controller {
        public IActionResult Packages() => View();
        public IActionResult Members() => View();
        public IActionResult Subscriptions() => View();
    }
}
