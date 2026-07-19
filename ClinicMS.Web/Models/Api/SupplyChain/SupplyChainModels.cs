namespace ClinicMS.Web.Models.Api.SupplyChain;

public record ProductCategoryDto(int Id, string CategoryName, string? Description, bool IsActive);

public record CreateProductCategoryRequest(string CategoryName, string? Description, bool IsActive);

public record UpdateProductCategoryRequest(string CategoryName, string? Description, bool IsActive);

public record ProductDto(int Id, int ProductCategoryId, string ProductCategoryName, string ProductName, string? Description, bool IsActive);

public record CreateProductRequest(int ProductCategoryId, string ProductName, string? Description, bool IsActive);

public record UpdateProductRequest(int ProductCategoryId, string ProductName, string? Description, bool IsActive);

public record ProductSkuDto(
    int Id, int ProductId, string ProductName, string SkuCode, string UnitName,
    decimal CostPrice, decimal SellingPrice, int StockQuantity, int ReorderLevel, bool IsActive);

public record CreateProductSkuRequest(
    int ProductId, string SkuCode, string UnitName, decimal CostPrice, decimal SellingPrice,
    int StockQuantity, int ReorderLevel, bool IsActive);

/// <summary>StockQuantity is deliberately absent here -- once a SKU exists, stock can only move
/// through the Stock Movements page so every change is logged.</summary>
public record UpdateProductSkuRequest(
    int ProductId, string SkuCode, string UnitName, decimal CostPrice, decimal SellingPrice,
    int ReorderLevel, bool IsActive);

public enum StockMovementType
{
    In,
    Out,
    Adjustment
}

public record StockMovementDto(
    int Id, int ProductSkuId, string SkuCode, string ProductName, StockMovementType MovementType,
    int Quantity, string? ReferenceId, DateTime MovementDate, string? Notes);

public record CreateStockMovementRequest(int ProductSkuId, StockMovementType MovementType, int Quantity, string? ReferenceId, string? Notes);

public record SupplierDto(int Id, string SupplierName, string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive);

public record CreateSupplierRequest(string SupplierName, string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive);

public record UpdateSupplierRequest(string SupplierName, string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive);

public enum PurchaseOrderStatus
{
    Draft,
    Ordered,
    Received,
    Cancelled
}

public record PurchaseOrderItemDto(int ProductSkuId, string SkuCode, string ProductName, int QuantityOrdered, int QuantityReceived, decimal UnitCost, decimal TotalCost);

public record PurchaseOrderItemRequest(int ProductSkuId, int QuantityOrdered, decimal UnitCost);

public record PurchaseOrderDto(
    int Id, string PoNumber, int SupplierId, string SupplierName, DateTime OrderDate,
    DateOnly? ExpectedDeliveryDate, decimal TotalAmount, PurchaseOrderStatus Status, string? Notes,
    IReadOnlyList<PurchaseOrderItemDto> Items);

public record CreatePurchaseOrderRequest(int SupplierId, DateOnly? ExpectedDeliveryDate, string? Notes, IReadOnlyList<PurchaseOrderItemRequest> Items);
