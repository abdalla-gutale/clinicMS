using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Expenses;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class ExpensesController : Controller
    {
        private readonly IExpensesApiClient _expensesApiClient;

        public ExpensesController(IExpensesApiClient expensesApiClient)
        {
            _expensesApiClient = expensesApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var expenses = await _expensesApiClient.GetAllAsync(cancellationToken);
            var categories = await _expensesApiClient.GetCategoriesAsync(cancellationToken);
            var vendors = await _expensesApiClient.GetVendorsAsync(cancellationToken);

            ViewBag.ExpensesJson = ViewJson.Serialize(expenses);
            ViewBag.CategoriesJson = ViewJson.Serialize(categories.Where(c => c.IsActive));
            ViewBag.VendorsJson = ViewJson.Serialize(vendors.Where(v => v.IsActive));

            return View();
        }

        public async Task<IActionResult> Categories(CancellationToken cancellationToken)
        {
            var categories = await _expensesApiClient.GetCategoriesAsync(cancellationToken);
            var expenses = await _expensesApiClient.GetAllAsync(cancellationToken);

            ViewBag.CategoriesJson = ViewJson.Serialize(categories);
            ViewBag.CategoryTotalsJson = ViewJson.Serialize(
                expenses.GroupBy(e => e.ExpenseCategoryId).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)));

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var expense = await _expensesApiClient.CreateAsync(request, cancellationToken);
                return Json(expense);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateExpenseCategoryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _expensesApiClient.CreateCategoryAsync(request, cancellationToken);
                return Json(category);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateExpenseCategoryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _expensesApiClient.UpdateCategoryAsync(id, request, cancellationToken);
                return Json(category);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _expensesApiClient.DeleteCategoryAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
