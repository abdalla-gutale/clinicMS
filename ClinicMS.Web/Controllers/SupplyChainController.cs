using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Expenses;
using ClinicMS.Web.Models.Api.SupplyChain;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class SupplyChainController : Controller
    {
        private readonly ISupplyChainApiClient _supplyChainApiClient;
        private readonly IExpensesApiClient _expensesApiClient;

        public SupplyChainController(ISupplyChainApiClient supplyChainApiClient, IExpensesApiClient expensesApiClient)
        {
            _supplyChainApiClient = supplyChainApiClient;
            _expensesApiClient = expensesApiClient;
        }

        // ── Product Categories ──
        public async Task<IActionResult> ProductCategories(CancellationToken cancellationToken)
        {
            var categories = await _supplyChainApiClient.GetProductCategoriesAsync(cancellationToken);
            ViewBag.ProductCategoriesJson = ViewJson.Serialize(categories);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProductCategory([FromBody] CreateProductCategoryRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.CreateProductCategoryAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProductCategory(int id, [FromBody] UpdateProductCategoryRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.UpdateProductCategoryAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProductCategory(int id, CancellationToken cancellationToken)
        {
            try { await _supplyChainApiClient.DeleteProductCategoryAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        // ── Products ──
        public async Task<IActionResult> Products(CancellationToken cancellationToken)
        {
            var products = await _supplyChainApiClient.GetProductsAsync(cancellationToken);
            var categories = await _supplyChainApiClient.GetProductCategoriesAsync(cancellationToken);
            ViewBag.ProductsJson = ViewJson.Serialize(products);
            ViewBag.ProductCategoriesJson = ViewJson.Serialize(categories.Where(c => c.IsActive));
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.CreateProductAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.UpdateProductAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
        {
            try { await _supplyChainApiClient.DeleteProductAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        // ── Product SKUs ──
        public async Task<IActionResult> ProductSkus(CancellationToken cancellationToken)
        {
            var skus = await _supplyChainApiClient.GetProductSkusAsync(cancellationToken);
            var products = await _supplyChainApiClient.GetProductsAsync(cancellationToken);
            ViewBag.ProductSkusJson = ViewJson.Serialize(skus);
            ViewBag.ProductsJson = ViewJson.Serialize(products.Where(p => p.IsActive));
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProductSku([FromBody] CreateProductSkuRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.CreateProductSkuAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProductSku(int id, [FromBody] UpdateProductSkuRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.UpdateProductSkuAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProductSku(int id, CancellationToken cancellationToken)
        {
            try { await _supplyChainApiClient.DeleteProductSkuAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        // ── Stock Movements ──
        public async Task<IActionResult> StockMovements(CancellationToken cancellationToken)
        {
            var movements = await _supplyChainApiClient.GetStockMovementsAsync(cancellationToken);
            var skus = await _supplyChainApiClient.GetProductSkusAsync(cancellationToken);
            ViewBag.StockMovementsJson = ViewJson.Serialize(movements);
            ViewBag.ProductSkusJson = ViewJson.Serialize(skus.Where(s => s.IsActive));
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStockMovement([FromBody] CreateStockMovementRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.CreateStockMovementAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        // ── Suppliers ──
        public async Task<IActionResult> Suppliers(CancellationToken cancellationToken)
        {
            var suppliers = await _supplyChainApiClient.GetSuppliersAsync(cancellationToken);
            ViewBag.SuppliersJson = ViewJson.Serialize(suppliers);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.CreateSupplierAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.UpdateSupplierAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int id, CancellationToken cancellationToken)
        {
            try { await _supplyChainApiClient.DeleteSupplierAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        // ── Purchase Orders ──
        public async Task<IActionResult> PurchaseOrders(CancellationToken cancellationToken)
        {
            var orders = await _supplyChainApiClient.GetPurchaseOrdersAsync(cancellationToken);
            var suppliers = await _supplyChainApiClient.GetSuppliersAsync(cancellationToken);
            var skus = await _supplyChainApiClient.GetProductSkusAsync(cancellationToken);
            ViewBag.PurchaseOrdersJson = ViewJson.Serialize(orders);
            ViewBag.SuppliersJson = ViewJson.Serialize(suppliers.Where(s => s.IsActive));
            ViewBag.ProductSkusJson = ViewJson.Serialize(skus.Where(s => s.IsActive));
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.CreatePurchaseOrderAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePurchaseOrderStatus(int id, [FromBody] UpdatePurchaseOrderStatusPayload request, CancellationToken cancellationToken)
        {
            try { return Json(await _supplyChainApiClient.UpdatePurchaseOrderStatusAsync(id, request.Status, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePurchaseOrder(int id, CancellationToken cancellationToken)
        {
            try { await _supplyChainApiClient.DeletePurchaseOrderAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        // ── Vendors (shared with Expenses/Recurring Expenses) ──
        public async Task<IActionResult> Vendors(CancellationToken cancellationToken)
        {
            var vendors = await _expensesApiClient.GetVendorsAsync(cancellationToken);
            ViewBag.VendorsJson = ViewJson.Serialize(vendors);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _expensesApiClient.CreateVendorAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] UpdateVendorRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _expensesApiClient.UpdateVendorAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVendor(int id, CancellationToken cancellationToken)
        {
            try { await _expensesApiClient.DeleteVendorAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }
    }

    public record UpdatePurchaseOrderStatusPayload(PurchaseOrderStatus Status);
}
