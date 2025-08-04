using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Validator for CreateSaleCommand that defines validation rules for sale creation.
/// </summary>
public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the CreateSaleCommandValidator with defined validation rules.
    /// </summary>
    /// <remarks>
    /// Validation rules include:
    /// - CustomerId: Must not be empty
    /// - CustomerName: Must not be empty and have reasonable length
    /// - CustomerEmail: Must be valid email format
    /// - BranchId: Must not be empty
    /// - BranchName: Must not be empty and have reasonable length
    /// - Items: Must have at least one item and each item must be valid
    /// </remarks>
    public CreateSaleValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required.");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .Length(1, 200)
            .WithMessage("Customer name must be between 1 and 200 characters.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .WithMessage("Customer email is required.")
            .EmailAddress()
            .WithMessage("Customer email must be a valid email address.")
            .MaximumLength(100)
            .WithMessage("Customer email must not exceed 100 characters.");

        RuleFor(x => x.BranchId)
            .NotEmpty()
            .WithMessage("Branch ID is required.");

        RuleFor(x => x.BranchName)
            .NotEmpty()
            .WithMessage("Branch name is required.")
            .Length(1, 200)
            .WithMessage("Branch name must be between 1 and 200 characters.");

        RuleFor(x => x.BranchLocation)
            .MaximumLength(500)
            .WithMessage("Branch location must not exceed 500 characters.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required for a sale.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateSaleItemValidator());
    }
}

/// <summary>
/// Validator for CreateSaleItemCommand that defines validation rules for sale item creation.
/// </summary>
public class CreateSaleItemValidator : AbstractValidator<CreateSaleItemCommand>
{
    /// <summary>
    /// Initializes a new instance of the CreateSaleItemCommandValidator with defined validation rules.
    /// </summary>
    /// <remarks>
    /// Validation rules include:
    /// - ProductId: Must not be empty
    /// - ProductName: Must not be empty and have reasonable length
    /// - ProductSku: Must not be empty and have reasonable length
    /// - UnitPrice: Must be greater than 0
    /// - Quantity: Must be between 1 and 20 (business rule)
    /// </remarks>
    public CreateSaleItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .Length(1, 200)
            .WithMessage("Product name must be between 1 and 200 characters.");

        RuleFor(x => x.ProductSku)
            .NotEmpty()
            .WithMessage("Product SKU is required.")
            .Length(1, 50)
            .WithMessage("Product SKU must be between 1 and 50 characters.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(20)
            .WithMessage("Cannot sell more than 20 identical items per sale.");
    }
}