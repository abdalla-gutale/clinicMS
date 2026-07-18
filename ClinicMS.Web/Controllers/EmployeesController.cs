using Microsoft.AspNetCore.Mvc;
namespace ClinicMS.Web.Controllers {
    public class EmployeesController : Controller {
        public IActionResult Index() => View();
        public IActionResult Types() => View();
    }
}
