using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.SupplyChain;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class PurchaseOrderTests
{
    private static async Task<(ClinicMsDbContext Db, DbSupplyChainApiClient Client, int SupplierId, int SkuId)> SeedAsync(int openingStock = 0, int reorderLevel = 5)
    {
        var db = TestDb.Create();
        var supplier = new SupplierEntity { SupplierName = "MedSupply Co.", IsActive = true };
        var category = new ProductCategoryEntity { CategoryName = "Skincare", IsActive = true };
        db.Suppliers.Add(supplier);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new ProductEntity { ProductCategoryId = category.Id, ProductName = "Vitamin C Serum", IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var sku = new ProductSkuEntity
        {
            ProductId = product.Id, SkuCode = "SKU-VCS-30", UnitName = "Bottle",
            CostPrice = 20m, SellingPrice = 35m, StockQuantity = openingStock, ReorderLevel = reorderLevel, IsActive = true,
        };
        db.ProductSkus.Add(sku);
        await db.SaveChangesAsync();

        return (db, new DbSupplyChainApiClient(db), supplier.Id, sku.Id);
    }

    [Fact]
    public async Task CreatePurchaseOrder_ComputesTotalAmountFromLineItems()
    {
        var (db, client, supplierId, skuId) = await SeedAsync();

        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);

        Assert.Equal(200m, order.TotalAmount);
        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
        db.Dispose();
    }

    [Fact]
    public async Task ReceivingAnOrder_IncreasesStockAndLogsAnInMovement()
    {
        var (db, client, supplierId, skuId) = await SeedAsync(openingStock: 5);

        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);

        await client.UpdatePurchaseOrderStatusAsync(order.Id, PurchaseOrderStatus.Received, default);

        var skus = await client.GetProductSkusAsync(default);
        var sku = Assert.Single(skus);
        Assert.Equal(15, sku.StockQuantity);

        var movements = await client.GetStockMovementsAsync(default);
        var movement = Assert.Single(movements);
        Assert.Equal(StockMovementType.In, movement.MovementType);
        Assert.Equal(10, movement.Quantity);
        db.Dispose();
    }

    [Fact]
    public async Task ChangingStatusOfAnAlreadyReceivedOrder_Throws()
    {
        var (db, client, supplierId, skuId) = await SeedAsync();
        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);
        await client.UpdatePurchaseOrderStatusAsync(order.Id, PurchaseOrderStatus.Received, default);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.UpdatePurchaseOrderStatusAsync(order.Id, PurchaseOrderStatus.Cancelled, default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task DeletingANonDraftOrder_Throws()
    {
        var (db, client, supplierId, skuId) = await SeedAsync();
        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);
        await client.UpdatePurchaseOrderStatusAsync(order.Id, PurchaseOrderStatus.Received, default);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.DeletePurchaseOrderAsync(order.Id, default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task PurchaseReturn_WithinReceivedQuantity_ReducesStockAndLogsAnOutMovement()
    {
        var (db, client, supplierId, skuId) = await SeedAsync();
        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);
        await client.UpdatePurchaseOrderStatusAsync(order.Id, PurchaseOrderStatus.Received, default);

        var purchaseReturn = await client.CreatePurchaseReturnAsync(new CreatePurchaseReturnRequest(
            order.Id, "Damaged in transit", new[] { new PurchaseReturnItemRequest(skuId, 4, 20m) }), default);

        Assert.Equal(80m, purchaseReturn.TotalAmount);

        var skus = await client.GetProductSkusAsync(default);
        Assert.Equal(6, Assert.Single(skus).StockQuantity); // 10 received - 4 returned

        var outMovement = (await client.GetStockMovementsAsync(default)).Single(m => m.MovementType == StockMovementType.Out);
        Assert.Equal(4, outMovement.Quantity);
        db.Dispose();
    }

    [Fact]
    public async Task PurchaseReturn_ExceedingReceivedQuantity_Throws()
    {
        var (db, client, supplierId, skuId) = await SeedAsync();
        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);
        await client.UpdatePurchaseOrderStatusAsync(order.Id, PurchaseOrderStatus.Received, default);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreatePurchaseReturnAsync(new CreatePurchaseReturnRequest(
            order.Id, null, new[] { new PurchaseReturnItemRequest(skuId, 11, 20m) }), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task PurchaseReturn_OnANonReceivedOrder_Throws()
    {
        var (db, client, supplierId, skuId) = await SeedAsync();
        var order = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
            supplierId, null, null, new[] { new PurchaseOrderItemRequest(skuId, 10, 20m) }), default);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreatePurchaseReturnAsync(new CreatePurchaseReturnRequest(
            order.Id, null, new[] { new PurchaseReturnItemRequest(skuId, 1, 20m) }), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }
}
