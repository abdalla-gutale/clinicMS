using ClinicMS.Web.Models.Api.SupplyChain;

namespace ClinicMS.Web.Services.Api;

public class SupplyChainApiClient : ApiClientBase, ISupplyChainApiClient
{
    public SupplyChainApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ProductCategoryDto>>("api/productcategories", cancellationToken);

    public Task<ProductCategoryDto> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ProductCategoryDto>("api/productcategories", request, cancellationToken);

    public Task<ProductCategoryDto> UpdateProductCategoryAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ProductCategoryDto>($"api/productcategories/{id}", request, cancellationToken);

    public Task DeleteProductCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/productcategories/{id}", cancellationToken);

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ProductDto>>("api/products", cancellationToken);

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ProductDto>("api/products", request, cancellationToken);

    public Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ProductDto>($"api/products/{id}", request, cancellationToken);

    public Task DeleteProductAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/products/{id}", cancellationToken);

    public Task<IReadOnlyList<ProductSkuDto>> GetProductSkusAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ProductSkuDto>>("api/productskus", cancellationToken);

    public Task<ProductSkuDto> CreateProductSkuAsync(CreateProductSkuRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ProductSkuDto>("api/productskus", request, cancellationToken);

    public Task<ProductSkuDto> UpdateProductSkuAsync(int id, UpdateProductSkuRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ProductSkuDto>($"api/productskus/{id}", request, cancellationToken);

    public Task DeleteProductSkuAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/productskus/{id}", cancellationToken);

    public Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<StockMovementDto>>("api/stockmovements", cancellationToken);

    public Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<StockMovementDto>("api/stockmovements", request, cancellationToken);

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<SupplierDto>>("api/suppliers", cancellationToken);

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SupplierDto>("api/suppliers", request, cancellationToken);

    public Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<SupplierDto>($"api/suppliers/{id}", request, cancellationToken);

    public Task DeleteSupplierAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/suppliers/{id}", cancellationToken);

    public Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<PurchaseOrderDto>>("api/purchaseorders", cancellationToken);

    public Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PurchaseOrderDto>("api/purchaseorders", request, cancellationToken);

    public Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(int id, PurchaseOrderStatus status, CancellationToken cancellationToken = default) =>
        PutAsync<PurchaseOrderDto>($"api/purchaseorders/{id}/status", new { status }, cancellationToken);

    public Task DeletePurchaseOrderAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/purchaseorders/{id}", cancellationToken);
}
