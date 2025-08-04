using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Validator for UpdateSaleCommand that defines validation rules for sale updates.
/// </summary>
public class UpdateSaleValidator : AbstractValidator<UpdateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the UpdateSaleCommandValidator with defined validation rules.
    /// </summary>
    public UpdateSaleValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Sale ID is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("Sale ID cannot be empty.");

        RuleForEach(x => x.ItemsToAdd)
            .SetValidator(new AddSaleItemValidator());

        RuleForEach(x => x.ItemsToUpdate)
            .SetValidator(new UpdateSaleItemValidator());

        RuleForEach(x => x.ProductIdsToRemove)
            .NotEqual(Guid.Empty)
            .WithMessage("Product ID to remove cannot be empty.");
    }
}

/// <summary>
/// Validator for AddSaleItemCommand
/// </summary>
public class AddSaleItemValidator : AbstractValidator<AddSaleItemCommand>
{
    public AddSaleItemValidator()
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

/// <summary>
/// Validator for UpdateSaleItemCommand
/// </summary>
public class UpdateSaleItemValidator : AbstractValidator<UpdateSaleItemCommand>
{
    public UpdateSaleItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(20)
            .WithMessage("Cannot sell more than 20 identical items per sale.");
    }
}