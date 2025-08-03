using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents an individual item within a sale.
/// This entity follows domain-driven design principles and includes business rules validation.
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>
    /// Gets the ID of the sale this item belongs to
    /// </summary>
    public Guid SaleId { get; set; }

    /// <summary>
    /// Gets the external product ID (External Identity pattern)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets the product name (denormalized data)
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the product SKU (denormalized data)
    /// </summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Gets the unit price of the product
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets the quantity of items
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets the discount percentage applied (0-100)
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets the discount amount in currency
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets the total amount for this item (after discount)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets whether this item has been cancelled
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Navigation property to the parent sale
    /// </summary>
    public Sale? Sale { get; set; }

    /// <summary>
    /// Initializes a new instance of the SaleItem class
    /// </summary>
    public SaleItem()
    {
        IsCancelled = false;
    }

    /// <summary>
    /// Creates a new sale item with the specified details
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="productName">The product name</param>
    /// <param name="productSku">The product SKU</param>
    /// <param name="unitPrice">The unit price</param>
    /// <param name="quantity">The quantity</param>
    public SaleItem(Guid productId, string productName, string productSku, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        ProductSku = productSku;
        UnitPrice = unitPrice;
        Quantity = quantity;
        IsCancelled = false;

        ApplyBusinessRules();
        CalculateTotal();
    }

    /// <summary>
    /// Updates the quantity and recalculates totals
    /// </summary>
    /// <param name="newQuantity">The new quantity</param>
    /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Quantity cannot be negative");

        if (newQuantity > 20)
            throw new ArgumentException("Cannot sell more than 20 identical items");

        if (IsCancelled)
            throw new InvalidOperationException("Cannot update quantity of a cancelled item");

        Quantity = newQuantity;
        ApplyBusinessRules();
        CalculateTotal();
    }

    /// <summary>
    /// Updates the unit price and recalculates totals
    /// </summary>
    /// <param name="newUnitPrice">The new unit price</param>
    /// <exception cref="ArgumentException">Thrown when price is invalid</exception>
    public void UpdateUnitPrice(decimal newUnitPrice)
    {
        if (newUnitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");

        if (IsCancelled)
            throw new InvalidOperationException("Cannot update price of a cancelled item");

        UnitPrice = newUnitPrice;
        ApplyBusinessRules();
        CalculateTotal();
    }

    /// <summary>
    /// Cancels this item
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
        TotalAmount = 0;
        DiscountAmount = 0;
        DiscountPercentage = 0;
    }

    /// <summary>
    /// Applies business rules for discount calculation
    /// Business Rules:
    /// - Purchases above 4 identical items have a 10% discount
    /// - Purchases between 10 and 20 identical items have a 20% discount
    /// - It's not possible to sell above 20 identical items
    /// - Purchases below 4 items cannot have a discount
    /// </summary>
    private void ApplyBusinessRules()
    {
        if (IsCancelled)
            return;

        // Reset discount
        DiscountPercentage = 0;

        // 20% discount for 10-20 items
        if (Quantity >= 10 && Quantity <= 20)
        {
            DiscountPercentage = 20; 
        }
        // 10% discount for 4-9 items
        else if (Quantity >= 4 && Quantity < 10)
        {
            DiscountPercentage = 10; 
        }
        // Below 4 items: no discount (0%)
        // Above 20 items: should be prevented at sale level
    }

    /// <summary>
    /// Calculates the total amount including discount
    /// </summary>
    private void CalculateTotal()
    {
        if (IsCancelled)
        {
            TotalAmount = 0;
            DiscountAmount = 0;
            return;
        }

        var subtotal = UnitPrice * Quantity;
        DiscountAmount = subtotal * (DiscountPercentage / 100);
        TotalAmount = subtotal - DiscountAmount;
    }

    /// <summary>
    /// Validates the sale item
    /// </summary>
    /// <returns>Validation result with any errors</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleItemValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }
}