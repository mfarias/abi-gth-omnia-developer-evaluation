using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Contains unit tests for the SaleItem entity class.
/// Tests cover discount calculation, business rules, quantity updates, and validation scenarios.
/// </summary>
public class SaleItemTests
{
    [Fact(DisplayName = "SaleItem should be created with correct values")]
    public void Given_ValidData_When_CreatingSaleItem_Then_ShouldSetCorrectValues()
    {
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var productSku = "SKU123";
        var unitPrice = 10.50m;
        var quantity = 3;

        var saleItem = new SaleItem(productId, productName, productSku, unitPrice, quantity);

        saleItem.ProductId.Should().Be(productId);
        saleItem.ProductName.Should().Be(productName);
        saleItem.ProductSku.Should().Be(productSku);
        saleItem.UnitPrice.Should().Be(unitPrice);
        saleItem.Quantity.Should().Be(quantity);
        saleItem.IsCancelled.Should().BeFalse();
        saleItem.TotalAmount.Should().Be(31.50m); // 3 * 10.50, no discount for qty < 4
        saleItem.DiscountPercentage.Should().Be(0);
        saleItem.DiscountAmount.Should().Be(0);
    }

    [Theory(DisplayName = "SaleItem should apply correct discount based on quantity")]
    [InlineData(1, 0, 0)] // No discount for 1 item
    [InlineData(3, 0, 0)] // No discount for 3 items
    [InlineData(4, 10, 4)] // 10% discount for 4 items: 40 * 0.1 = 4
    [InlineData(5, 10, 5)] // 10% discount for 5 items: 50 * 0.1 = 5
    [InlineData(9, 10, 9)] // 10% discount for 9 items: 90 * 0.1 = 9
    [InlineData(10, 20, 20)] // 20% discount for 10 items: 100 * 0.2 = 20
    [InlineData(15, 20, 30)] // 20% discount for 15 items: 150 * 0.2 = 30
    [InlineData(20, 20, 40)] // 20% discount for 20 items: 200 * 0.2 = 40
    public void Given_DifferentQuantities_When_CreatingSaleItem_Then_ShouldApplyCorrectDiscount(
        int quantity, decimal expectedDiscountPercentage, decimal expectedDiscountAmount)
    {
        var unitPrice = 10m;

        var saleItem = new SaleItem(Guid.NewGuid(), "Product", "SKU", unitPrice, quantity);

        saleItem.DiscountPercentage.Should().Be(expectedDiscountPercentage);
        saleItem.DiscountAmount.Should().Be(expectedDiscountAmount);
        
        var expectedSubtotal = unitPrice * quantity;
        var expectedTotal = expectedSubtotal - expectedDiscountAmount;
        saleItem.TotalAmount.Should().Be(expectedTotal);
    }

    [Fact(DisplayName = "UpdateQuantity should recalculate discount and total")]
    public void Given_SaleItem_When_UpdatingQuantity_Then_ShouldRecalculateDiscountAndTotal()
    {
        var saleItem = new SaleItem(Guid.NewGuid(), "Product", "SKU", 10m, 3); // No discount initially
        saleItem.TotalAmount.Should().Be(30m); // Verify initial state

        saleItem.UpdateQuantity(5); // Should get 10% discount

        saleItem.Quantity.Should().Be(5);
        saleItem.DiscountPercentage.Should().Be(10);
        saleItem.DiscountAmount.Should().Be(5m); // 50 * 0.1
        saleItem.TotalAmount.Should().Be(45m); // 50 - 5
    }

    [Fact(DisplayName = "UpdateQuantity should throw exception for negative quantity")]
    public void Given_SaleItem_When_UpdatingToNegativeQuantity_Then_ShouldThrowException()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();

        var act = () => saleItem.UpdateQuantity(-1);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Quantity cannot be negative");
    }

    [Fact(DisplayName = "UpdateQuantity should throw exception for quantity above maximum")]
    public void Given_SaleItem_When_UpdatingToQuantityAboveMax_Then_ShouldThrowException()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();

        var act = () => saleItem.UpdateQuantity(21);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cannot sell more than 20 identical items");
    }

    [Fact(DisplayName = "UpdateQuantity should throw exception for cancelled item")]
    public void Given_CancelledSaleItem_When_UpdatingQuantity_Then_ShouldThrowException()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();
        saleItem.Cancel();

        var act = () => saleItem.UpdateQuantity(5);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update quantity of a cancelled item");
    }

    [Fact(DisplayName = "UpdateUnitPrice should recalculate total")]
    public void Given_SaleItem_When_UpdatingUnitPrice_Then_ShouldRecalculateTotal()
    {
        var saleItem = new SaleItem(Guid.NewGuid(), "Product", "SKU", 10m, 5); // 10% discount
        var originalTotal = saleItem.TotalAmount;

        saleItem.UpdateUnitPrice(20m);

        saleItem.UnitPrice.Should().Be(20m);
        saleItem.DiscountAmount.Should().Be(10m); // 100 * 0.1
        saleItem.TotalAmount.Should().Be(90m); // 100 - 10
        saleItem.TotalAmount.Should().NotBe(originalTotal);
    }

    [Fact(DisplayName = "UpdateUnitPrice should throw exception for negative price")]
    public void Given_SaleItem_When_UpdatingToNegativePrice_Then_ShouldThrowException()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();

        var act = () => saleItem.UpdateUnitPrice(-1m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unit price cannot be negative");
    }

    [Fact(DisplayName = "UpdateUnitPrice should throw exception for cancelled item")]
    public void Given_CancelledSaleItem_When_UpdatingUnitPrice_Then_ShouldThrowException()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();
        saleItem.Cancel();

        var act = () => saleItem.UpdateUnitPrice(15m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update price of a cancelled item");
    }

    [Fact(DisplayName = "Cancel should set item as cancelled and reset amounts")]
    public void Given_SaleItem_When_Cancelled_Then_ShouldSetCancelledAndResetAmounts()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();
        saleItem.TotalAmount.Should().BeGreaterThan(0); // Verify it has value initially

        saleItem.Cancel();

        saleItem.IsCancelled.Should().BeTrue();
        saleItem.TotalAmount.Should().Be(0);
        saleItem.DiscountAmount.Should().Be(0);
        saleItem.DiscountPercentage.Should().Be(0);
    }

    [Fact(DisplayName = "Cancelled item should not apply business rules")]
    public void Given_CancelledSaleItem_When_ApplyingBusinessRules_Then_ShouldNotApplyDiscount()
    {
        var saleItem = new SaleItem(Guid.NewGuid(), "Product", "SKU", 10m, 10); // Should have 20% discount
        saleItem.DiscountPercentage.Should().Be(20); // Verify discount is applied

        saleItem.Cancel();

        saleItem.DiscountPercentage.Should().Be(0);
        saleItem.DiscountAmount.Should().Be(0);
        saleItem.TotalAmount.Should().Be(0);
    }

    [Fact(DisplayName = "Validate should return valid result for valid sale item")]
    public void Given_ValidSaleItem_When_Validated_Then_ShouldReturnValid()
    {
        var saleItem = SaleItemTestData.GenerateValidSaleItem();

        var result = saleItem.Validate();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory(DisplayName = "Business rules should be applied correctly for edge cases")]
    [InlineData(4, 10)] // Minimum for 10% discount
    [InlineData(9, 10)] // Maximum for 10% discount
    [InlineData(10, 20)] // Minimum for 20% discount
    [InlineData(20, 20)] // Maximum for 20% discount
    public void Given_EdgeCaseQuantities_When_CreatingSaleItem_Then_ShouldApplyCorrectDiscount(
        int quantity, decimal expectedDiscountPercentage)
    {
        var saleItem = new SaleItem(Guid.NewGuid(), "Product", "SKU", 10m, quantity);

        saleItem.DiscountPercentage.Should().Be(expectedDiscountPercentage);
    }

    [Fact(DisplayName = "Multiple quantity updates should maintain business rules")]
    public void Given_SaleItem_When_UpdatingQuantityMultipleTimes_Then_ShouldMaintainBusinessRules()
    {
        var saleItem = new SaleItem(Guid.NewGuid(), "Product", "SKU", 10m, 1);
        saleItem.DiscountPercentage.Should().Be(0); // Initially no discount

        saleItem.UpdateQuantity(5); //Update to get 10% discount
        saleItem.DiscountPercentage.Should().Be(10);
        saleItem.TotalAmount.Should().Be(45m); // 50 - 5

        saleItem.UpdateQuantity(12); //Update to get 20% discount
        saleItem.DiscountPercentage.Should().Be(20);
        saleItem.TotalAmount.Should().Be(96m); // 120 - 24

        saleItem.UpdateQuantity(2); //Update back to no discount
        saleItem.DiscountPercentage.Should().Be(0);
        saleItem.TotalAmount.Should().Be(20m); // 20 - 0
    }
}