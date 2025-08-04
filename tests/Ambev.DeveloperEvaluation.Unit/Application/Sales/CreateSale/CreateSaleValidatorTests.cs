using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for the <see cref="CreateSaleValidator"/> class.
/// Tests cover validation scenarios for CreateSaleCommand.
/// </summary>
public class CreateSaleValidatorTests
{
    private readonly CreateSaleValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleValidatorTests"/> class.
    /// </summary>
    public CreateSaleValidatorTests()
    {
        _validator = new CreateSaleValidator();
    }

    /// <summary>
    /// Tests that a valid create sale command passes validation.
    /// </summary>
    [Fact(DisplayName = "Given valid create sale command When validating Then should pass")]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a command with empty customer ID fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with empty customer ID When validating Then should fail")]
    public void Validate_EmptyCustomerId_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.CustomerId = Guid.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer ID is required.");
    }

    /// <summary>
    /// Tests that a command with empty customer name fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with empty customer name When validating Then should fail")]
    public void Validate_EmptyCustomerName_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.CustomerName = string.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer name is required.");
    }

    /// <summary>
    /// Tests that a command with invalid email fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with invalid email When validating Then should fail")]
    public void Validate_InvalidEmail_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.CustomerEmail = "invalid-email";

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer email must be a valid email address.");
    }

    /// <summary>
    /// Tests that a command with empty branch ID fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with empty branch ID When validating Then should fail")]
    public void Validate_EmptyBranchId_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.BranchId = Guid.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Branch ID is required.");
    }

    /// <summary>
    /// Tests that a command with no items fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with no items When validating Then should fail")]
    public void Validate_NoItems_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.Items.Clear();

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "At least one item is required for a sale.");
    }

    /// <summary>
    /// Tests that a command with customer name exceeding max length fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with long customer name When validating Then should fail")]
    public void Validate_LongCustomerName_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.CustomerName = new string('x', 201); // Exceeds 200 character limit

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer name must be between 1 and 200 characters.");
    }

    /// <summary>
    /// Tests that a command with email exceeding max length fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with long email When validating Then should fail")]
    public void Validate_LongEmail_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.CustomerEmail = new string('x', 95) + "@test.com"; // Exceeds 100 character limit

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer email must not exceed 100 characters.");
    }

    /// <summary>
    /// Tests that a command with branch location exceeding max length fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with long branch location When validating Then should fail")]
    public void Validate_LongBranchLocation_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.BranchLocation = new string('x', 501); // Exceeds 500 character limit

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Branch location must not exceed 500 characters.");
    }
}

/// <summary>
/// Contains unit tests for the <see cref="CreateSaleItemValidator"/> class.
/// Tests cover validation scenarios for CreateSaleItemCommand.
/// </summary>
public class CreateSaleItemValidatorTests
{
    private readonly CreateSaleItemValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleItemValidatorTests"/> class.
    /// </summary>
    public CreateSaleItemValidatorTests()
    {
        _validator = new CreateSaleItemValidator();
    }

    /// <summary>
    /// Tests that a valid create sale item command passes validation.
    /// </summary>
    [Fact(DisplayName = "Given valid create sale item command When validating Then should pass")]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleItemCommand();

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a command with empty product ID fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with empty product ID When validating Then should fail")]
    public void Validate_EmptyProductId_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleItemCommand();
        command.ProductId = Guid.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Product ID is required.");
    }

    /// <summary>
    /// Tests that a command with zero or negative unit price fails validation.
    /// </summary>
    [Theory(DisplayName = "Given command with invalid unit price When validating Then should fail")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Validate_InvalidUnitPrice_ShouldFail(decimal unitPrice)
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleItemCommand();
        command.UnitPrice = unitPrice;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Unit price must be greater than 0.");
    }

    /// <summary>
    /// Tests that a command with quantity exceeding maximum fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with quantity above maximum When validating Then should fail")]
    public void Validate_QuantityAboveMaximum_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleItemCommand();
        command.Quantity = 21; // Exceeds maximum of 20

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Cannot sell more than 20 identical items per sale.");
    }

    /// <summary>
    /// Tests that a command with zero or negative quantity fails validation.
    /// </summary>
    [Theory(DisplayName = "Given command with invalid quantity When validating Then should fail")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Validate_InvalidQuantity_ShouldFail(int quantity)
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleItemCommand();
        command.Quantity = quantity;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity must be greater than 0.");
    }
}