using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for the <see cref="UpdateSaleValidator"/> class.
/// Tests cover validation scenarios for UpdateSaleCommand.
/// </summary>
public class UpdateSaleValidatorTests
{
    private readonly UpdateSaleValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleValidatorTests"/> class.
    /// </summary>
    public UpdateSaleValidatorTests()
    {
        _validator = new UpdateSaleValidator();
    }

    /// <summary>
    /// Tests that a valid update sale command passes validation.
    /// </summary>
    [Fact(DisplayName = "Given valid update sale command When validating Then should pass")]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a command with empty ID fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with empty ID When validating Then should fail")]
    public void Validate_EmptyId_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.Id = Guid.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Sale ID cannot be empty.");
    }

    /// <summary>
    /// Tests that a command with invalid items to add fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with invalid items to add When validating Then should fail")]
    public void Validate_InvalidItemsToAdd_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd = new List<AddSaleItemCommand>
        {
            new AddSaleItemCommand
            {
                ProductId = Guid.Empty,
                ProductName = "",
                UnitPrice = -1,
                Quantity = 0
            }
        };

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that a command with invalid items to update fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with invalid items to update When validating Then should fail")]
    public void Validate_InvalidItemsToUpdate_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToUpdate = new List<UpdateSaleItemCommand>
        {
            new UpdateSaleItemCommand
            {
                ProductId = Guid.Empty,
                Quantity = 0
            }
        };

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that a command with empty product IDs to remove fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with empty product IDs to remove When validating Then should fail")]
    public void Validate_EmptyProductIdsToRemove_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ProductIdsToRemove = new List<Guid> { Guid.Empty };

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Product ID to remove cannot be empty.");
    }
}

/// <summary>
/// Contains unit tests for the <see cref="AddSaleItemValidator"/> class.
/// Tests cover validation scenarios for AddSaleItemCommand.
/// </summary>
public class AddSaleItemValidatorTests
{
    private readonly AddSaleItemValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddSaleItemValidatorTests"/> class.
    /// </summary>
    public AddSaleItemValidatorTests()
    {
        _validator = new AddSaleItemValidator();
    }

    /// <summary>
    /// Tests that a valid add sale item command passes validation.
    /// </summary>
    [Fact(DisplayName = "Given valid add sale item command When validating Then should pass")]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Given
        var command = SalesTestData.GenerateValidAddSaleItemCommand();

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
        var command = SalesTestData.GenerateValidAddSaleItemCommand();
        command.ProductId = Guid.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Product ID is required.");
    }

    /// <summary>
    /// Tests that a command with invalid unit price fails validation.
    /// </summary>
    [Theory(DisplayName = "Given command with invalid unit price When validating Then should fail")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Validate_InvalidUnitPrice_ShouldFail(decimal unitPrice)
    {
        // Given
        var command = SalesTestData.GenerateValidAddSaleItemCommand();
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
        var command = SalesTestData.GenerateValidAddSaleItemCommand();
        command.Quantity = 21; // Exceeds maximum of 20

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Cannot sell more than 20 identical items per sale.");
    }
}

/// <summary>
/// Contains unit tests for the <see cref="UpdateSaleItemValidator"/> class.
/// Tests cover validation scenarios for UpdateSaleItemCommand.
/// </summary>
public class UpdateSaleItemValidatorTests
{
    private readonly UpdateSaleItemValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleItemValidatorTests"/> class.
    /// </summary>
    public UpdateSaleItemValidatorTests()
    {
        _validator = new UpdateSaleItemValidator();
    }

    /// <summary>
    /// Tests that a valid update sale item command passes validation.
    /// </summary>
    [Fact(DisplayName = "Given valid update sale item command When validating Then should pass")]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleItemCommand();

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
        var command = SalesTestData.GenerateValidUpdateSaleItemCommand();
        command.ProductId = Guid.Empty;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Product ID is required.");
    }

    /// <summary>
    /// Tests that a command with quantity exceeding maximum fails validation.
    /// </summary>
    [Fact(DisplayName = "Given command with quantity above maximum When validating Then should fail")]
    public void Validate_QuantityAboveMaximum_ShouldFail()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleItemCommand();
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
        var command = SalesTestData.GenerateValidUpdateSaleItemCommand();
        command.Quantity = quantity;

        // When
        var result = _validator.Validate(command);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity must be greater than 0.");
    }
}