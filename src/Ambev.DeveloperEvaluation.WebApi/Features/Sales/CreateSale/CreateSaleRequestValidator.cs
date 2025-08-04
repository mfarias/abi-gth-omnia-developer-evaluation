using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
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
            .SetValidator(new CreateSaleItemRequestValidator());
    }
}

public class CreateSaleItemRequestValidator : AbstractValidator<CreateSaleItemRequest>
{
    public CreateSaleItemRequestValidator()
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