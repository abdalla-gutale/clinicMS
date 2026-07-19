using ClinicMS.Web.Models.Api.SupplyChain;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockSupplyChainApiClient : ISupplyChainApiClient
{
    public Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductCategoryDto>>(MockStore.ProductCategories.ToList());

    public Task<ProductCategoryDto> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new ProductCategoryDto(MockStore.NextProductCategoryId++, request.CategoryName, request.Description, request.IsActive);
        MockStore.ProductCategories.Add(category);
        return Task.FromResult(category);
    }

    public Task<ProductCategoryDto> UpdateProductCategoryAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.ProductCategories.FindIndex(c => c.Id == id);
        if (index < 0) throw new ApiException(404, "Product category not found.");

        var updated = new ProductCategoryDto(id, request.CategoryName, request.Description, request.IsActive);
        MockStore.ProductCategories[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteProductCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.Products.Any(p => p.ProductCategoryId == id))
        {
            throw new ApiException(400, "Cannot delete a category that still has products assigned to it.");
        }
        if (MockStore.ProductCategories.RemoveAll(c => c.Id == id) == 0)
        {
            throw new ApiException(404, "Product category not found.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductDto>>(MockStore.Products.ToList());

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var category = MockStore.ProductCategories.FirstOrDefault(c => c.Id == request.ProductCategoryId)
            ?? throw new ApiException(400, "Selected product category does not exist.");

        var product = new ProductDto(MockStore.NextProductId++, category.Id, category.CategoryName, request.ProductName, request.Description, request.IsActive);
        MockStore.Products.Add(product);
        return Task.FromResult(product);
    }

    public Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Products.FindIndex(p => p.Id == id);
        if (index < 0) throw new ApiException(404, "Product not found.");

        var category = MockStore.ProductCategories.FirstOrDefault(c => c.Id == request.ProductCategoryId)
            ?? throw new ApiException(400, "Selected product category does not exist.");

        var updated = new ProductDto(id, category.Id, category.CategoryName, request.ProductName, request.Description, request.IsActive);
        MockStore.Products[index] = updated;

        // Keep denormalized ProductName on any SKUs of this product in sync.
        for (var i = 0; i < MockStore.ProductSkus.Count; i++)
        {
            var sku = MockStore.ProductSkus[i];
            if (sku.ProductId == id && sku.ProductName != updated.ProductName)
            {
                MockStore.ProductSkus[i] = sku with { ProductName = updated.ProductName };
            }
        }

        return Task.FromResult(updated);
    }

    public Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.ProductSkus.Any(s => s.ProductId == id))
        {
            throw new ApiException(400, "Cannot delete a product that still has SKUs assigned to it.");
        }
        if (MockStore.Products.RemoveAll(p => p.Id == id) == 0)
        {
            throw new ApiException(404, "Product not found.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductSkuDto>> GetProductSkusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductSkuDto>>(MockStore.ProductSkus.ToList());

    public Task<ProductSkuDto> CreateProductSkuAsync(CreateProductSkuRequest request, CancellationToken cancellationToken = default)
    {
        var product = MockStore.Products.FirstOrDefault(p => p.Id == request.ProductId)
            ?? throw new ApiException(400, "Selected product does not exist.");
        if (MockStore.ProductSkus.Any(s => s.SkuCode.Equals(request.SkuCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ApiException(400, "A SKU with this code already exists.");
        }

        var sku = new ProductSkuDto(
            MockStore.NextProductSkuId++, product.Id, product.ProductName, request.SkuCode, request.UnitName,
            request.CostPrice, request.SellingPrice, request.StockQuantity, request.ReorderLevel, request.IsActive);
        MockStore.ProductSkus.Add(sku);
        return Task.FromResult(sku);
    }

    public Task<ProductSkuDto> UpdateProductSkuAsync(int id, UpdateProductSkuRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.ProductSkus.FindIndex(s => s.Id == id);
        if (index < 0) throw new ApiException(404, "Product SKU not found.");

        var product = MockStore.Products.FirstOrDefault(p => p.Id == request.ProductId)
            ?? throw new ApiException(400, "Selected product does not exist.");
        if (MockStore.ProductSkus.Any(s => s.Id != id && s.SkuCode.Equals(request.SkuCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ApiException(400, "A SKU with this code already exists.");
        }

        var existing = MockStore.ProductSkus[index];
        var updated = existing with
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            SkuCode = request.SkuCode,
            UnitName = request.UnitName,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
        };
        MockStore.ProductSkus[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteProductSkuAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.ProductSkus.RemoveAll(s => s.Id == id) == 0)
        {
            throw new ApiException(404, "Product SKU not found.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockMovementDto>>(MockStore.StockMovements.OrderByDescending(m => m.MovementDate).ToList());

    public Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementRequest request, CancellationToken cancellationToken = default)
    {
        var skuIndex = MockStore.ProductSkus.FindIndex(s => s.Id == request.ProductSkuId);
        if (skuIndex < 0) throw new ApiException(400, "Selected SKU does not exist.");

        var sku = MockStore.ProductSkus[skuIndex];
        int newQuantity;
        switch (request.MovementType)
        {
            case StockMovementType.In:
                if (request.Quantity <= 0) throw new ApiException(400, "Quantity must be greater than zero for a stock-in movement.");
                newQuantity = sku.StockQuantity + request.Quantity;
                break;
            case StockMovementType.Out:
                if (request.Quantity <= 0) throw new ApiException(400, "Quantity must be greater than zero for a stock-out movement.");
                if (request.Quantity > sku.StockQuantity) throw new ApiException(400, $"Cannot remove {request.Quantity} units -- only {sku.StockQuantity} in stock.");
                newQuantity = sku.StockQuantity - request.Quantity;
                break;
            case StockMovementType.Adjustment:
                if (request.Quantity == 0) throw new ApiException(400, "Adjustment quantity cannot be zero.");
                newQuantity = sku.StockQuantity + request.Quantity;
                if (newQuantity < 0) throw new ApiException(400, "Adjustment would result in negative stock.");
                break;
            default:
                throw new ApiException(400, "Unknown movement type.");
        }

        MockStore.ProductSkus[skuIndex] = sku with { StockQuantity = newQuantity };

        var movement = new StockMovementDto(
            MockStore.NextStockMovementId++, sku.Id, sku.SkuCode, sku.ProductName, request.MovementType,
            request.Quantity, request.ReferenceId, DateTime.UtcNow, request.Notes);
        MockStore.StockMovements.Add(movement);
        return Task.FromResult(movement);
    }

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupplierDto>>(MockStore.Suppliers.ToList());

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new SupplierDto(MockStore.NextSupplierId++, request.SupplierName, request.ContactPerson, request.Phone, request.Email, request.Address, request.IsActive);
        MockStore.Suppliers.Add(supplier);
        return Task.FromResult(supplier);
    }

    public Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Suppliers.FindIndex(s => s.Id == id);
        if (index < 0) throw new ApiException(404, "Supplier not found.");

        var updated = new SupplierDto(id, request.SupplierName, request.ContactPerson, request.Phone, request.Email, request.Address, request.IsActive);
        MockStore.Suppliers[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteSupplierAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.PurchaseOrders.Any(po => po.SupplierId == id))
        {
            throw new ApiException(400, "Cannot delete a supplier that has purchase orders on record.");
        }
        if (MockStore.Suppliers.RemoveAll(s => s.Id == id) == 0)
        {
            throw new ApiException(404, "Supplier not found.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PurchaseOrderDto>>(MockStore.PurchaseOrders.OrderByDescending(po => po.OrderDate).ToList());

    public Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = MockStore.Suppliers.FirstOrDefault(s => s.Id == request.SupplierId)
            ?? throw new ApiException(400, "Selected supplier does not exist.");
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ApiException(400, "A purchase order must have at least one item.");
        }

        var items = new List<PurchaseOrderItemDto>();
        foreach (var itemRequest in request.Items)
        {
            var sku = MockStore.ProductSkus.FirstOrDefault(s => s.Id == itemRequest.ProductSkuId)
                ?? throw new ApiException(400, "One of the selected SKUs does not exist.");
            if (itemRequest.QuantityOrdered <= 0) throw new ApiException(400, "Quantity ordered must be greater than zero.");
            if (itemRequest.UnitCost < 0) throw new ApiException(400, "Unit cost cannot be negative.");

            items.Add(new PurchaseOrderItemDto(sku.Id, sku.SkuCode, sku.ProductName, itemRequest.QuantityOrdered, 0, itemRequest.UnitCost, itemRequest.QuantityOrdered * itemRequest.UnitCost));
        }

        var order = new PurchaseOrderDto(
            MockStore.NextPurchaseOrderId++, $"PO-{MockStore.NextPurchaseOrderNumber++}", supplier.Id, supplier.SupplierName,
            DateTime.UtcNow, request.ExpectedDeliveryDate, items.Sum(i => i.TotalCost), PurchaseOrderStatus.Draft, request.Notes, items);
        MockStore.PurchaseOrders.Add(order);
        return Task.FromResult(order);
    }

    public Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(int id, PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PurchaseOrders.FindIndex(po => po.Id == id);
        if (index < 0) throw new ApiException(404, "Purchase order not found.");

        var order = MockStore.PurchaseOrders[index];
        if (order.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
        {
            throw new ApiException(400, $"Purchase order is already {order.Status} and cannot change status.");
        }

        var items = order.Items;
        if (status == PurchaseOrderStatus.Received)
        {
            var received = new List<PurchaseOrderItemDto>();
            foreach (var item in order.Items)
            {
                var skuIndex = MockStore.ProductSkus.FindIndex(s => s.Id == item.ProductSkuId);
                if (skuIndex >= 0)
                {
                    var sku = MockStore.ProductSkus[skuIndex];
                    MockStore.ProductSkus[skuIndex] = sku with { StockQuantity = sku.StockQuantity + item.QuantityOrdered };
                    MockStore.StockMovements.Add(new StockMovementDto(
                        MockStore.NextStockMovementId++, sku.Id, sku.SkuCode, sku.ProductName, StockMovementType.In,
                        item.QuantityOrdered, order.PoNumber, DateTime.UtcNow, "Purchase order received"));
                }
                received.Add(item with { QuantityReceived = item.QuantityOrdered });
            }
            items = received;
        }

        var updated = order with { Status = status, Items = items };
        MockStore.PurchaseOrders[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeletePurchaseOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = MockStore.PurchaseOrders.FirstOrDefault(po => po.Id == id)
            ?? throw new ApiException(404, "Purchase order not found.");
        if (order.Status != PurchaseOrderStatus.Draft)
        {
            throw new ApiException(400, "Only draft purchase orders can be deleted.");
        }

        MockStore.PurchaseOrders.Remove(order);
        return Task.CompletedTask;
    }
}
