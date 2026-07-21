using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.SupplyChain;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbSupplyChainApiClient : ISupplyChainApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbSupplyChainApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    // ----- Product Categories -----

    public async Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.ProductCategories.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        return categories.Select(c => new ProductCategoryDto(c.Id, c.CategoryName, c.Description, c.IsActive)).ToList();
    }

    public async Task<ProductCategoryDto> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ProductCategoryEntity { CategoryName = request.CategoryName, Description = request.Description, IsActive = request.IsActive };
        _db.ProductCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductCategoryDto(entity.Id, entity.CategoryName, entity.Description, entity.IsActive);
    }

    public async Task<ProductCategoryDto> UpdateProductCategoryAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Product category not found.");

        entity.CategoryName = request.CategoryName;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductCategoryDto(entity.Id, entity.CategoryName, entity.Description, entity.IsActive);
    }

    public async Task DeleteProductCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.Products.AnyAsync(p => p.ProductCategoryId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a category that still has products assigned to it.");
        }

        var entity = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Product category not found.");
        _db.ProductCategories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Products -----

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.OrderBy(p => p.Id).ToListAsync(cancellationToken);
        var categories = await _db.ProductCategories.ToDictionaryAsync(c => c.Id, c => c.CategoryName, cancellationToken);
        return products.Select(p => new ProductDto(p.Id, p.ProductCategoryId, categories.GetValueOrDefault(p.ProductCategoryId, ""), p.ProductName, p.Description, p.IsActive)).ToList();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == request.ProductCategoryId, cancellationToken)
            ?? throw new ApiException(400, "Selected product category does not exist.");

        var entity = new ProductEntity { ProductCategoryId = category.Id, ProductName = request.ProductName, Description = request.Description, IsActive = request.IsActive };
        _db.Products.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductDto(entity.Id, category.Id, category.CategoryName, entity.ProductName, entity.Description, entity.IsActive);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Product not found.");

        var category = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == request.ProductCategoryId, cancellationToken)
            ?? throw new ApiException(400, "Selected product category does not exist.");

        entity.ProductCategoryId = category.Id;
        entity.ProductName = request.ProductName;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductDto(entity.Id, category.Id, category.CategoryName, entity.ProductName, entity.Description, entity.IsActive);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.ProductSkus.AnyAsync(s => s.ProductId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a product that still has SKUs assigned to it.");
        }

        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Product not found.");
        _db.Products.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Product SKUs -----

    public async Task<IReadOnlyList<ProductSkuDto>> GetProductSkusAsync(CancellationToken cancellationToken = default)
    {
        var skus = await _db.ProductSkus.OrderBy(s => s.Id).ToListAsync(cancellationToken);
        var products = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);
        return skus.Select(s => ToDto(s, products.GetValueOrDefault(s.ProductId, ""))).ToList();
    }

    public async Task<ProductSkuDto> CreateProductSkuAsync(CreateProductSkuRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new ApiException(400, "Selected product does not exist.");

        if (await _db.ProductSkus.AnyAsync(s => s.SkuCode == request.SkuCode, cancellationToken))
        {
            throw new ApiException(400, "A SKU with this code already exists.");
        }

        if (request.CostPrice < 0 || request.SellingPrice < 0)
        {
            throw new ApiException(400, "Cost price and selling price cannot be negative.");
        }

        var entity = new ProductSkuEntity
        {
            ProductId = product.Id,
            SkuCode = request.SkuCode,
            UnitName = request.UnitName,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
        };
        _db.ProductSkus.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.StockQuantity > 0)
        {
            _db.StockMovements.Add(new StockMovementEntity
            {
                ProductSkuId = entity.Id,
                MovementType = "In",
                Quantity = request.StockQuantity,
                ReferenceId = null,
                MovementDate = DateTime.UtcNow,
                Notes = "Initial stock on SKU creation",
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(entity, product.ProductName);
    }

    public async Task<ProductSkuDto> UpdateProductSkuAsync(int id, UpdateProductSkuRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ProductSkus.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Product SKU not found.");

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new ApiException(400, "Selected product does not exist.");

        if (await _db.ProductSkus.AnyAsync(s => s.Id != id && s.SkuCode == request.SkuCode, cancellationToken))
        {
            throw new ApiException(400, "A SKU with this code already exists.");
        }

        if (request.CostPrice < 0 || request.SellingPrice < 0)
        {
            throw new ApiException(400, "Cost price and selling price cannot be negative.");
        }

        // StockQuantity is deliberately not settable here -- stock only ever changes through a
        // recorded Stock Movement (or a PO receipt), so every change stays auditable.
        entity.ProductId = product.Id;
        entity.SkuCode = request.SkuCode;
        entity.UnitName = request.UnitName;
        entity.CostPrice = request.CostPrice;
        entity.SellingPrice = request.SellingPrice;
        entity.ReorderLevel = request.ReorderLevel;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, product.ProductName);
    }

    public async Task DeleteProductSkuAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.PurchaseOrderItems.AnyAsync(i => i.ProductSkuId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a SKU that appears on a purchase order.");
        }

        var entity = await _db.ProductSkus.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Product SKU not found.");

        var movements = await _db.StockMovements.Where(m => m.ProductSkuId == id).ToListAsync(cancellationToken);
        _db.StockMovements.RemoveRange(movements);
        _db.ProductSkus.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Stock Movements -----

    public async Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(CancellationToken cancellationToken = default)
    {
        var movements = await _db.StockMovements.OrderByDescending(m => m.MovementDate).ToListAsync(cancellationToken);
        var skus = await _db.ProductSkus.ToDictionaryAsync(s => s.Id, cancellationToken);
        var products = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);

        return movements.Select(m =>
        {
            var sku = skus.GetValueOrDefault(m.ProductSkuId);
            return new StockMovementDto(
                m.Id, m.ProductSkuId, sku?.SkuCode ?? "", sku is not null ? products.GetValueOrDefault(sku.ProductId, "") : "",
                Enum.TryParse<StockMovementType>(m.MovementType, out var type) ? type : StockMovementType.Adjustment,
                m.Quantity, m.ReferenceId, m.MovementDate, m.Notes);
        }).ToList();
    }

    public async Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementRequest request, CancellationToken cancellationToken = default)
    {
        var sku = await _db.ProductSkus.FirstOrDefaultAsync(s => s.Id == request.ProductSkuId, cancellationToken)
            ?? throw new ApiException(400, "Selected SKU does not exist.");

        switch (request.MovementType)
        {
            case StockMovementType.In:
                if (request.Quantity <= 0)
                {
                    throw new ApiException(400, "Quantity must be greater than zero for a stock-in movement.");
                }
                sku.StockQuantity += request.Quantity;
                break;
            case StockMovementType.Out:
                if (request.Quantity <= 0)
                {
                    throw new ApiException(400, "Quantity must be greater than zero for a stock-out movement.");
                }
                if (request.Quantity > sku.StockQuantity)
                {
                    throw new ApiException(400, $"Cannot remove {request.Quantity} units -- only {sku.StockQuantity} in stock.");
                }
                sku.StockQuantity -= request.Quantity;
                break;
            case StockMovementType.Adjustment:
                if (request.Quantity == 0)
                {
                    throw new ApiException(400, "Adjustment quantity cannot be zero.");
                }
                if (sku.StockQuantity + request.Quantity < 0)
                {
                    throw new ApiException(400, "Adjustment would result in negative stock.");
                }
                sku.StockQuantity += request.Quantity;
                break;
            default:
                throw new ApiException(400, "Unknown movement type.");
        }

        var entity = new StockMovementEntity
        {
            ProductSkuId = sku.Id,
            MovementType = request.MovementType.ToString(),
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            MovementDate = DateTime.UtcNow,
            Notes = request.Notes,
        };
        _db.StockMovements.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == sku.ProductId, cancellationToken);
        return new StockMovementDto(entity.Id, sku.Id, sku.SkuCode, product?.ProductName ?? "", request.MovementType, entity.Quantity, entity.ReferenceId, entity.MovementDate, entity.Notes);
    }

    // ----- Suppliers -----

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _db.Suppliers.OrderBy(s => s.Id).ToListAsync(cancellationToken);
        return suppliers.Select(ToDto).ToList();
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SupplierEntity
        {
            SupplierName = request.SupplierName,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            IsActive = request.IsActive,
        };
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Supplier not found.");

        entity.SupplierName = request.SupplierName;
        entity.ContactPerson = request.ContactPerson;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteSupplierAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.PurchaseOrders.AnyAsync(po => po.SupplierId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a supplier that has purchase orders on record.");
        }

        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Supplier not found.");
        _db.Suppliers.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Purchase Orders -----

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _db.PurchaseOrders.OrderByDescending(po => po.OrderDate).ToListAsync(cancellationToken);
        return await BuildPurchaseOrderDtosAsync(orders, cancellationToken);
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new ApiException(400, "Selected supplier does not exist.");

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ApiException(400, "A purchase order must have at least one item.");
        }

        var skuIds = request.Items.Select(i => i.ProductSkuId).ToList();
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);

        var totalAmount = 0m;
        foreach (var item in request.Items)
        {
            if (!skus.ContainsKey(item.ProductSkuId))
            {
                throw new ApiException(400, "One of the selected SKUs does not exist.");
            }
            if (item.QuantityOrdered <= 0)
            {
                throw new ApiException(400, "Quantity ordered must be greater than zero.");
            }
            if (item.UnitCost < 0)
            {
                throw new ApiException(400, "Unit cost cannot be negative.");
            }
            totalAmount += item.QuantityOrdered * item.UnitCost;
        }

        var order = new PurchaseOrderEntity
        {
            PoNumber = await NextPoNumberAsync(cancellationToken),
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            TotalAmount = totalAmount,
            Status = "Draft",
            Notes = request.Notes,
        };
        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var item in request.Items)
        {
            _db.PurchaseOrderItems.Add(new PurchaseOrderItemEntity
            {
                PurchaseOrderId = order.Id,
                ProductSkuId = item.ProductSkuId,
                QuantityOrdered = item.QuantityOrdered,
                QuantityReceived = 0,
                UnitCost = item.UnitCost,
                TotalCost = item.QuantityOrdered * item.UnitCost,
            });
        }
        await _db.SaveChangesAsync(cancellationToken);

        return (await BuildPurchaseOrderDtosAsync(new List<PurchaseOrderEntity> { order }, cancellationToken)).Single();
    }

    public async Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(int id, PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Purchase order not found.");

        var currentStatus = ParseStatus(order.Status);
        if (currentStatus is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
        {
            throw new ApiException(400, $"Purchase order is already {currentStatus} and cannot change status.");
        }

        var items = await _db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == id).ToListAsync(cancellationToken);

        if (status == PurchaseOrderStatus.Received)
        {
            var skuIds = items.Select(i => i.ProductSkuId).ToList();
            var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);

            foreach (var item in items)
            {
                if (skus.TryGetValue(item.ProductSkuId, out var sku))
                {
                    sku.StockQuantity += item.QuantityOrdered;
                    _db.StockMovements.Add(new StockMovementEntity
                    {
                        ProductSkuId = sku.Id,
                        MovementType = "In",
                        Quantity = item.QuantityOrdered,
                        ReferenceId = order.PoNumber,
                        MovementDate = DateTime.UtcNow,
                        Notes = "Purchase order received",
                    });
                }
                item.QuantityReceived = item.QuantityOrdered;
            }
        }

        order.Status = status.ToString();
        await _db.SaveChangesAsync(cancellationToken);

        return (await BuildPurchaseOrderDtosAsync(new List<PurchaseOrderEntity> { order }, cancellationToken)).Single();
    }

    public async Task DeletePurchaseOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Purchase order not found.");

        if (ParseStatus(order.Status) != PurchaseOrderStatus.Draft)
        {
            throw new ApiException(400, "Only draft purchase orders can be deleted.");
        }

        var items = await _db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == id).ToListAsync(cancellationToken);
        _db.PurchaseOrderItems.RemoveRange(items);
        _db.PurchaseOrders.Remove(order);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Purchase Returns -----

    public async Task<IReadOnlyList<PurchaseReturnDto>> GetPurchaseReturnsAsync(CancellationToken cancellationToken = default)
    {
        var returns = await _db.PurchaseReturns.OrderByDescending(r => r.ReturnDate).ToListAsync(cancellationToken);
        return await BuildPurchaseReturnDtosAsync(returns, cancellationToken);
    }

    public async Task<PurchaseReturnDto> CreatePurchaseReturnAsync(CreatePurchaseReturnRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == request.PurchaseOrderId, cancellationToken)
            ?? throw new ApiException(400, "Selected purchase order does not exist.");
        if (ParseStatus(order.Status) != PurchaseOrderStatus.Received)
        {
            throw new ApiException(400, "Only received purchase orders can have items returned.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ApiException(400, "A purchase return must have at least one item.");
        }

        var orderItems = await _db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == order.Id).ToListAsync(cancellationToken);

        var priorReturnIds = await _db.PurchaseReturns.Where(r => r.PurchaseOrderId == order.Id).Select(r => r.Id).ToListAsync(cancellationToken);
        var alreadyReturned = await _db.PurchaseReturnItems
            .Where(i => priorReturnIds.Contains(i.PurchaseReturnId))
            .GroupBy(i => i.ProductSkuId)
            .Select(g => new { ProductSkuId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.ProductSkuId, x => x.Quantity, cancellationToken);

        var skuIds = request.Items.Select(i => i.ProductSkuId).ToList();
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);

        var purchaseReturn = new PurchaseReturnEntity
        {
            PurchaseOrderId = order.Id,
            SupplierId = order.SupplierId,
            ReturnDate = DateTime.UtcNow,
            TotalAmount = 0m,
            Reason = request.Reason,
        };
        _db.PurchaseReturns.Add(purchaseReturn);
        await _db.SaveChangesAsync(cancellationToken);

        var totalAmount = 0m;
        foreach (var itemRequest in request.Items)
        {
            var orderItem = orderItems.FirstOrDefault(i => i.ProductSkuId == itemRequest.ProductSkuId)
                ?? throw new ApiException(400, "One of the selected SKUs is not on this purchase order.");

            var returnedSoFar = alreadyReturned.GetValueOrDefault(itemRequest.ProductSkuId, 0);
            var returnable = orderItem.QuantityReceived - returnedSoFar;
            if (itemRequest.Quantity <= 0 || itemRequest.Quantity > returnable)
            {
                throw new ApiException(400, $"Cannot return {itemRequest.Quantity} units of SKU {orderItem.ProductSkuId} -- only {returnable} available to return.");
            }

            if (!skus.TryGetValue(itemRequest.ProductSkuId, out var sku))
            {
                throw new ApiException(400, "One of the selected SKUs does not exist.");
            }

            if (itemRequest.Quantity > sku.StockQuantity)
            {
                throw new ApiException(400, $"Cannot return {itemRequest.Quantity} of '{sku.SkuCode}' -- only {sku.StockQuantity} currently in stock.");
            }

            sku.StockQuantity -= itemRequest.Quantity;
            _db.StockMovements.Add(new StockMovementEntity
            {
                ProductSkuId = sku.Id,
                MovementType = "Out",
                Quantity = itemRequest.Quantity,
                ReferenceId = order.PoNumber,
                MovementDate = DateTime.UtcNow,
                Notes = "Purchase return to supplier",
            });

            var totalCost = itemRequest.Quantity * itemRequest.UnitCost;
            _db.PurchaseReturnItems.Add(new PurchaseReturnItemEntity
            {
                PurchaseReturnId = purchaseReturn.Id,
                ProductSkuId = itemRequest.ProductSkuId,
                Quantity = itemRequest.Quantity,
                UnitCost = itemRequest.UnitCost,
                TotalCost = totalCost,
            });
            totalAmount += totalCost;
        }

        purchaseReturn.TotalAmount = totalAmount;
        await _db.SaveChangesAsync(cancellationToken);

        return (await BuildPurchaseReturnDtosAsync(new List<PurchaseReturnEntity> { purchaseReturn }, cancellationToken)).Single();
    }

    private async Task<List<PurchaseReturnDto>> BuildPurchaseReturnDtosAsync(List<PurchaseReturnEntity> returns, CancellationToken cancellationToken)
    {
        if (returns.Count == 0)
        {
            return new List<PurchaseReturnDto>();
        }

        var returnIds = returns.Select(r => r.Id).ToList();
        var orderIds = returns.Select(r => r.PurchaseOrderId).Distinct().ToList();
        var supplierIds = returns.Select(r => r.SupplierId).Distinct().ToList();

        var orders = await _db.PurchaseOrders.Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken);
        var suppliers = await _db.Suppliers.Where(s => supplierIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var items = await _db.PurchaseReturnItems.Where(i => returnIds.Contains(i.PurchaseReturnId)).ToListAsync(cancellationToken);
        var skuIds = items.Select(i => i.ProductSkuId).Distinct().ToList();
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var productIds = skus.Values.Select(s => s.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);

        return returns.Select(r =>
        {
            var returnItems = items.Where(i => i.PurchaseReturnId == r.Id).Select(i =>
            {
                var sku = skus.GetValueOrDefault(i.ProductSkuId);
                var productName = sku is not null ? products.GetValueOrDefault(sku.ProductId, "") : "";
                return new PurchaseReturnItemDto(i.ProductSkuId, sku?.SkuCode ?? "", productName, i.Quantity, i.UnitCost, i.TotalCost);
            }).ToList();

            return new PurchaseReturnDto(
                r.Id, r.PurchaseOrderId, orders.GetValueOrDefault(r.PurchaseOrderId)?.PoNumber ?? "",
                r.SupplierId, suppliers.GetValueOrDefault(r.SupplierId)?.SupplierName ?? "",
                r.ReturnDate, r.TotalAmount, r.Reason, returnItems);
        }).ToList();
    }

    private async Task<string> NextPoNumberAsync(CancellationToken cancellationToken)
    {
        var sequence = await _db.IdSequences.FirstOrDefaultAsync(s => s.SequenceKey == "poNumber", cancellationToken);
        if (sequence is null)
        {
            return $"PO-{Guid.NewGuid():N}"[..8];
        }

        var number = $"{sequence.Prefix}{sequence.NextValue.ToString().PadLeft(sequence.PaddingLength, '0')}";
        sequence.NextValue++;
        sequence.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return number;
    }

    /// <summary>Statuses this app never writes (e.g. a stray legacy "Pending" row) fall back to Draft
    /// -- the least-privileged state -- rather than crashing or hiding the order.</summary>
    private static PurchaseOrderStatus ParseStatus(string status) =>
        Enum.TryParse<PurchaseOrderStatus>(status, out var parsed) ? parsed : PurchaseOrderStatus.Draft;

    private async Task<List<PurchaseOrderDto>> BuildPurchaseOrderDtosAsync(List<PurchaseOrderEntity> orders, CancellationToken cancellationToken)
    {
        if (orders.Count == 0)
        {
            return new List<PurchaseOrderDto>();
        }

        var orderIds = orders.Select(o => o.Id).ToList();
        var supplierIds = orders.Select(o => o.SupplierId).Distinct().ToList();

        var suppliers = await _db.Suppliers.Where(s => supplierIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var items = await _db.PurchaseOrderItems.Where(i => orderIds.Contains(i.PurchaseOrderId)).ToListAsync(cancellationToken);
        var skuIds = items.Select(i => i.ProductSkuId).Distinct().ToList();
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var productIds = skus.Values.Select(s => s.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);

        return orders.Select(o =>
        {
            var orderItems = items.Where(i => i.PurchaseOrderId == o.Id).Select(i =>
            {
                var sku = skus.GetValueOrDefault(i.ProductSkuId);
                var productName = sku is not null ? products.GetValueOrDefault(sku.ProductId, "") : "";
                return new PurchaseOrderItemDto(i.ProductSkuId, sku?.SkuCode ?? "", productName, i.QuantityOrdered, i.QuantityReceived, i.UnitCost, i.TotalCost);
            }).ToList();

            return new PurchaseOrderDto(
                o.Id, o.PoNumber, o.SupplierId, suppliers.GetValueOrDefault(o.SupplierId)?.SupplierName ?? "",
                o.OrderDate, o.ExpectedDeliveryDate, o.TotalAmount, ParseStatus(o.Status), o.Notes, orderItems);
        }).ToList();
    }

    private static ProductSkuDto ToDto(ProductSkuEntity e, string productName) => new(
        e.Id, e.ProductId, productName, e.SkuCode, e.UnitName, e.CostPrice, e.SellingPrice, e.StockQuantity, e.ReorderLevel, e.IsActive);

    private static SupplierDto ToDto(SupplierEntity e) => new(e.Id, e.SupplierName, e.ContactPerson, e.Phone, e.Email, e.Address, e.IsActive);
}
