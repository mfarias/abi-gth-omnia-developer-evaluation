using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "Sale should be created with default values")]
    public void Given_NewSale_When_Created_Then_ShouldHaveDefaultValues()
    {
        var sale = new Sale();

        sale.Status.Should().Be(SaleStatus.Confirmed);
        sale.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sale.SaleDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sale.SaleNumber.Should().NotBeNullOrEmpty();
        sale.Items.Should().BeEmpty();
        sale.TotalAmount.Should().Be(0);
    }

    [Fact(DisplayName = "Sale should generate unique sale numbers")]
    public void Given_MultipleSales_When_Created_Then_ShouldHaveUniqueSaleNumbers()
    {
        var sale1 = new Sale();
        var sale2 = new Sale();

        sale1.SaleNumber.Should().NotBe(sale2.SaleNumber);
        sale1.SaleNumber.Should().StartWith("SALE-");
        sale2.SaleNumber.Should().StartWith("SALE-");
    }

    [Fact(DisplayName = "AddItem should add new item and recalculate total")]
    public void Given_Sale_When_AddingNewItem_Then_ShouldAddItemAndRecalculateTotal()
    {
        var sale = SaleTestData.GenerateValidSale();
        var item = SaleItemTestData.GenerateValidSaleItem();
        var originalItemCount = sale.Items.Count;

        sale.AddItem(item);

        sale.Items.Should().HaveCount(originalItemCount + 1);
        sale.Items.Should().Contain(item);
        sale.TotalAmount.Should().Be(item.TotalAmount);
        sale.UpdatedAt.Should().NotBeNull();
        item.SaleId.Should().Be(sale.Id);
    }

    [Fact(DisplayName = "AddItem should merge quantities for existing product")]
    public void Given_SaleWithExistingProduct_When_AddingSameProduct_Then_ShouldMergeQuantities()
    {
        var sale = SaleTestData.GenerateValidSale();
        var productId = SaleItemTestData.GenerateValidProductId();
        var item1 = new SaleItem(productId, "Product", "SKU123", 10m, 2);
        var item2 = new SaleItem(productId, "Product", "SKU123", 10m, 3);
        
        sale.AddItem(item1);
        var originalItemCount = sale.Items.Count;

        sale.AddItem(item2);

        sale.Items.Should().HaveCount(originalItemCount);
        var mergedItem = sale.Items.First(i => i.ProductId == productId);
        mergedItem.Quantity.Should().Be(5); // 2 + 3
        mergedItem.TotalAmount.Should().Be(45m); // 5 * 10 with 10% discount = 50 - 5 = 45
    }

    [Fact(DisplayName = "AddItem should throw exception when adding to cancelled sale")]
    public void Given_CancelledSale_When_AddingItem_Then_ShouldThrowException()
    {
        var sale = SaleTestData.GenerateCancelledSale();
        var item = SaleItemTestData.GenerateValidSaleItem();

        var act = () => sale.AddItem(item);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add items to a cancelled sale");
    }

    [Fact(DisplayName = "AddItem should throw exception when exceeding maximum quantity")]
    public void Given_SaleWithMaxQuantity_When_AddingMoreItems_Then_ShouldThrowException()
    {
        var sale = SaleTestData.GenerateValidSale();
        var productId = SaleItemTestData.GenerateValidProductId();
        var item1 = new SaleItem(productId, "Product", "SKU123", 10m, 20);
        var item2 = new SaleItem(productId, "Product", "SKU123", 10m, 1);
        
        sale.AddItem(item1);

        var act = () => sale.AddItem(item2);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cannot sell more than 20 identical items");
    }

    [Fact(DisplayName = "RemoveItem should remove item and recalculate total")]
    public void Given_SaleWithItems_When_RemovingItem_Then_ShouldRemoveAndRecalculateTotal()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(2);
        var itemToRemove = sale.Items.First();
        var productId = itemToRemove.ProductId;
        var originalCount = sale.Items.Count;

        sale.RemoveItem(productId);

        sale.Items.Should().HaveCount(originalCount - 1);
        sale.Items.Should().NotContain(i => i.ProductId == productId);
        sale.TotalAmount.Should().Be(sale.Items.Sum(i => i.TotalAmount));
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "RemoveItem should do nothing when product not found")]
    public void Given_Sale_When_RemovingNonExistentItem_Then_ShouldDoNothing()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        var nonExistentProductId = Guid.NewGuid();
        var originalCount = sale.Items.Count;
        var originalTotal = sale.TotalAmount;

        sale.RemoveItem(nonExistentProductId);

        sale.Items.Should().HaveCount(originalCount);
        sale.TotalAmount.Should().Be(originalTotal);
    }

    [Fact(DisplayName = "RemoveItem should throw exception when removing from cancelled sale")]
    public void Given_CancelledSale_When_RemovingItem_Then_ShouldThrowException()
    {
        var sale = SaleTestData.GenerateCancelledSale();
        var productId = SaleItemTestData.GenerateValidProductId();

        var act = () => sale.RemoveItem(productId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot remove items from a cancelled sale");
    }

    [Fact(DisplayName = "UpdateItemQuantity should update quantity and recalculate total")]
    public void Given_SaleWithItem_When_UpdatingQuantity_Then_ShouldUpdateAndRecalculate()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        var item = sale.Items.First();
        var productId = item.ProductId;
        var newQuantity = 5;

        sale.UpdateItemQuantity(productId, newQuantity);

        var updatedItem = sale.Items.First(i => i.ProductId == productId);
        updatedItem.Quantity.Should().Be(newQuantity);
        sale.TotalAmount.Should().Be(sale.Items.Sum(i => i.TotalAmount));
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "UpdateItemQuantity should remove item when quantity is zero")]
    public void Given_SaleWithItem_When_UpdatingQuantityToZero_Then_ShouldRemoveItem()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        var item = sale.Items.First();
        var productId = item.ProductId;
        var originalCount = sale.Items.Count;


        sale.UpdateItemQuantity(productId, 0);


        sale.Items.Should().HaveCount(originalCount - 1);
        sale.Items.Should().NotContain(i => i.ProductId == productId);
    }

    [Fact(DisplayName = "UpdateItemQuantity should throw exception when exceeding maximum")]
    public void Given_SaleWithItem_When_UpdatingQuantityAboveMax_Then_ShouldThrowException()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        var item = sale.Items.First();
        var productId = item.ProductId;

        var act = () => sale.UpdateItemQuantity(productId, 21);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot sell more than 20 identical items");
    }

    [Fact(DisplayName = "UpdateItemQuantity should throw exception when updating cancelled sale")]
    public void Given_CancelledSale_When_UpdatingItemQuantity_Then_ShouldThrowException()
    {
        var sale = SaleTestData.GenerateCancelledSale();
        var productId = SaleItemTestData.GenerateValidProductId();

        var act = () => sale.UpdateItemQuantity(productId, 5);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update items in a cancelled sale");
    }

    [Fact(DisplayName = "Cancel should set status to cancelled and update timestamp")]
    public void Given_Sale_When_Cancelled_Then_ShouldSetStatusAndTimestamp()
    {
        var sale = SaleTestData.GenerateValidSale();
        var originalUpdatedAt = sale.UpdatedAt;

        sale.Cancel();

        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.UpdatedAt.Should().NotBe(originalUpdatedAt);
        sale.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Validate should return valid result for valid sale")]
    public void Given_ValidSale_When_Validated_Then_ShouldReturnValid()
    {
        var sale = SaleTestData.GenerateValidSale();

        var result = sale.Validate();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory(DisplayName = "Sale should recalculate total correctly with multiple items")]
    [InlineData(1, 10.0, 1, 15.0, 25.0)] // No discounts: 1*10 + 1*15 = 25
    [InlineData(4, 10.0, 5, 20.0, 126.0)] // 10% discounts: (4*10*0.9) + (5*20*0.9) = 36 + 90 = 126
    [InlineData(10, 5.0, 15, 8.0, 136.0)] // 20% discounts: (10*5*0.8) + (15*8*0.8) = 40 + 96 = 136
    public void Given_SaleWithMultipleItems_When_AddingItems_Then_ShouldCalculateTotalCorrectly(
        int quantity1, decimal unitPrice1, int quantity2, decimal unitPrice2, decimal expectedTotal)
    {
        var sale = SaleTestData.GenerateValidSale();
        var item1 = new SaleItem(Guid.NewGuid(), "Product1", "SKU1", unitPrice1, quantity1);
        var item2 = new SaleItem(Guid.NewGuid(), "Product2", "SKU2", unitPrice2, quantity2);

        sale.AddItem(item1);
        sale.AddItem(item2);

        sale.TotalAmount.Should().Be(expectedTotal);
    }
}