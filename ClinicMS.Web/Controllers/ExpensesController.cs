using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Expenses;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class ExpensesController : Controller
    {
        private const int DefaultPageSize = 8;

        private readonly IExpensesApiClient _expensesApiClient;
        private readonly ISettingsApiClient _settingsApiClient;

        public ExpensesController(IExpensesApiClient expensesApiClient, ISettingsApiClient settingsApiClient)
        {
            _expensesApiClient = expensesApiClient;
            _settingsApiClient = settingsApiClient;
        }

        [RequirePermission("/expenses", PermissionAction.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var initialPage = await _expensesApiClient.GetPagedAsync(1, DefaultPageSize, null, null, null, cancellationToken);
            var categories = await _expensesApiClient.GetCategoriesAsync(cancellationToken);
            var vendors = await _expensesApiClient.GetVendorsAsync(cancellationToken);
            var accounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            var budgetSummary = await _expensesApiClient.GetBudgetSummaryAsync(cancellationToken);

            ViewBag.ExpensesPageJson = ViewJson.Serialize(initialPage);
            ViewBag.CategoriesJson = ViewJson.Serialize(categories.Where(c => c.IsActive));
            ViewBag.VendorsJson = ViewJson.Serialize(vendors.Where(v => v.IsActive));
            ViewBag.PaymentAccountsJson = ViewJson.Serialize(accounts.Where(a => a.IsActive));
            ViewBag.BudgetSummaryJson = ViewJson.Serialize(budgetSummary);

            return View();
        }

        [HttpGet]
        [RequirePermission("/expenses", PermissionAction.View)]
        public async Task<IActionResult> GetPage(int page, int pageSize, string? search, string? category, string? paymentMethod, CancellationToken cancellationToken)
        {
            var result = await _expensesApiClient.GetPagedAsync(page, pageSize, search, category, paymentMethod, cancellationToken);
            return Json(result);
        }

        // One consolidated page (Categories / Vendors / Budget Estimation / Setup Expenses tabs)
        // replaces what used to be three separate pages -- see /Expenses/Setup.cshtml.
        [RequirePermission("/expense-setup", PermissionAction.View)]
        public async Task<IActionResult> Setup(CancellationToken cancellationToken)
        {
            var categories = await _expensesApiClient.GetCategoriesAsync(cancellationToken);
            var expenses = await _expensesApiClient.GetAllAsync(cancellationToken);
            var vendors = await _expensesApiClient.GetVendorsAsync(cancellationToken);
            var accounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            var budgetSummary = await _expensesApiClient.GetBudgetSummaryAsync(cancellationToken);
            var schedules = await _expensesApiClient.GetRecurringSchedulesAsync(cancellationToken);

            ViewBag.CategoriesJson = ViewJson.Serialize(categories);
            ViewBag.CategoryTotalsJson = ViewJson.Serialize(
                expenses.GroupBy(e => e.ExpenseCategoryId).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)));
            ViewBag.VendorsJson = ViewJson.Serialize(vendors);
            ViewBag.PaymentAccountsJson = ViewJson.Serialize(accounts);
            ViewBag.BudgetSummaryJson = ViewJson.Serialize(budgetSummary);
            ViewBag.RecurringExpensesJson = ViewJson.Serialize(schedules);

            return View();
        }

        [HttpPost]
        [RequirePermission("/recurring-expense-schedules", PermissionAction.Create)]
        public async Task<IActionResult> CreateRecurringExpense([FromBody] CreateRecurringExpenseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var schedule = await _expensesApiClient.CreateRecurringScheduleAsync(request, cancellationToken);
                return Json(schedule);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/recurring-expense-schedules", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateRecurringExpense(int id, [FromBody] UpdateRecurringExpenseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var schedule = await _expensesApiClient.UpdateRecurringScheduleAsync(id, request, cancellationToken);
                return Json(schedule);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/recurring-expense-schedules", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteRecurringExpense(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _expensesApiClient.DeleteRecurringScheduleAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/expenses", PermissionAction.Create)]
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
        [RequirePermission("/expenses", PermissionAction.Create)]
        public async Task<IActionResult> GenerateRecurring(CancellationToken cancellationToken)
        {
            var generated = await _expensesApiClient.GenerateDueRecurringExpensesAsync(cancellationToken);
            return Json(new { generated });
        }

        [RequirePermission("/expenses", PermissionAction.View)]
        public async Task<IActionResult> BudgetSummary(CancellationToken cancellationToken)
        {
            var summary = await _expensesApiClient.GetBudgetSummaryAsync(cancellationToken);
            return Json(summary);
        }

        [HttpPost]
        [RequirePermission("/expenses", PermissionAction.Edit)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var expense = await _expensesApiClient.UpdateAsync(id, request, cancellationToken);
                return Json(expense);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/expenses", PermissionAction.Delete)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _expensesApiClient.DeleteAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/expense-categories", PermissionAction.Create)]
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
        [RequirePermission("/expense-categories", PermissionAction.Edit)]
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
        [RequirePermission("/expense-categories", PermissionAction.Delete)]
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
