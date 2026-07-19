using ClinicMS.Web.Models.Api.SupplyChain;

namespace ClinicMS.Web.Services.Api;

public interface ISupplyChainApiClient
{
    Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductCategoryDto> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ProductCategoryDto> UpdateProductCategoryAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductSkuDto>> GetProductSkusAsync(CancellationToken cancellationToken = default);
    Task<ProductSkuDto> CreateProductSkuAsync(CreateProductSkuRequest request, CancellationToken cancellationToken = default);
    Task<ProductSkuDto> UpdateProductSkuAsync(int id, UpdateProductSkuRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductSkuAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(CancellationToken cancellationToken = default);
    Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task DeleteSupplierAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(int id, PurchaseOrderStatus status, CancellationToken cancellationToken = default);
    Task DeletePurchaseOrderAsync(int id, CancellationToken cancellationToken = default);
}
