using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Contains unit tests for the <see cref="UpdateSaleHandler"/> class.
/// </summary>
public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILogger<UpdateSaleHandler> _logger;
    private readonly UpdateSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleHandlerTests"/> class.
    /// Sets up the test dependencies and creates fake data generators.
    /// </summary>
    public UpdateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _logger = Substitute.For<ILogger<UpdateSaleHandler>>();
        _handler = new UpdateSaleHandler(_saleRepository, _logger);
    }

    /// <summary>
    /// Tests that a valid update sale request is handled successfully.
    /// </summary>
    [Fact(DisplayName = "Given valid update request When updating sale Then returns success response")]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        var updatedSale = SaleTestData.GenerateValidSaleWithItems(2);

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(updatedSale.Id);
        result.SaleNumber.Should().Be(updatedSale.SaleNumber);
        result.TotalAmount.Should().Be(updatedSale.TotalAmount);
        result.Status.Should().Be(updatedSale.Status);
        await _saleRepository.Received(1).GetByIdAsync(command.Id, Arg.Any<CancellationToken>());
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that an invalid update sale request throws a validation exception.
    /// </summary>
    [Fact(DisplayName = "Given invalid update data When updating sale Then throws validation exception")]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateInvalidUpdateSaleCommand();

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that updating a non-existent sale throws KeyNotFoundException.
    /// </summary>
    [Fact(DisplayName = "Given non-existent sale ID When updating sale Then throws KeyNotFoundException")]
    public async Task Handle_NonExistentSale_ThrowsKeyNotFoundException()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {command.Id} not found");
    }

    /// <summary>
    /// Tests that updating a cancelled sale throws InvalidOperationException.
    /// </summary>
    [Fact(DisplayName = "Given cancelled sale When updating sale Then throws InvalidOperationException")]
    public async Task Handle_CancelledSale_ThrowsInvalidOperationException()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        var cancelledSale = SaleTestData.GenerateCancelledSale();
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(cancelledSale);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Cannot update cancelled sale {cancelledSale.SaleNumber}");
    }

    /// <summary>
    /// Tests that adding items to sale works correctly.
    /// </summary>
    [Fact(DisplayName = "Given items to add When updating sale Then adds items successfully")]
    public async Task Handle_AddItems_AddsItemsSuccessfully()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd = SalesTestData.GenerateAddSaleItemCommands(2);
        command.ItemsToUpdate.Clear();
        command.ProductIdsToRemove.Clear();
        
        var sale = SaleTestData.GenerateValidSale();
        var updatedSale = SaleTestData.GenerateValidSaleWithItems(2);

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Message.Should().Contain("Added");
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that updating item quantities works correctly.
    /// </summary>
    [Fact(DisplayName = "Given items to update When updating sale Then updates quantities successfully")]
    public async Task Handle_UpdateItems_UpdatesQuantitiesSuccessfully()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd.Clear();
        command.ItemsToUpdate = SalesTestData.GenerateUpdateSaleItemCommands(1);
        command.ProductIdsToRemove.Clear();
        
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        // Set the product ID to match what we're trying to update
        var existingItem = sale.Items.First();
        command.ItemsToUpdate.First().ProductId = existingItem.ProductId;
        
        var updatedSale = SaleTestData.GenerateValidSaleWithItems(1);

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Message.Should().Contain("Updated quantity");
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that removing items from sale works correctly.
    /// </summary>
    [Fact(DisplayName = "Given items to remove When updating sale Then removes items successfully")]
    public async Task Handle_RemoveItems_RemovesItemsSuccessfully()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd.Clear();
        command.ItemsToUpdate.Clear();
        
        var sale = SaleTestData.GenerateValidSaleWithItems(2);
        var existingItem = sale.Items.First();
        command.ProductIdsToRemove = new List<Guid> { existingItem.ProductId };
        
        var updatedSale = SaleTestData.GenerateValidSaleWithItems(1);

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Message.Should().Contain("Removed product");
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that multiple operations can be performed in a single update.
    /// </summary>
    [Fact(DisplayName = "Given multiple operations When updating sale Then performs all operations")]
    public async Task Handle_MultipleOperations_PerformsAllOperations()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd = SalesTestData.GenerateAddSaleItemCommands(1);
        command.ItemsToUpdate = SalesTestData.GenerateUpdateSaleItemCommands(1);
        
        var sale = SaleTestData.GenerateValidSaleWithItems(2);
        var existingItem = sale.Items.First();
        command.ItemsToUpdate.First().ProductId = existingItem.ProductId;
        command.ProductIdsToRemove = new List<Guid> { sale.Items.Last().ProductId };
        
        var updatedSale = SaleTestData.GenerateValidSaleWithItems(2);

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Message.Should().Contain("Added");
        result.Message.Should().Contain("Updated quantity");
        result.Message.Should().Contain("Removed product");
        await _saleRepository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that repository is called with correct parameters.
    /// </summary>
    [Fact(DisplayName = "Given valid command When handling Then calls repository methods correctly")]
    public async Task Handle_ValidRequest_CallsRepositoryCorrectly()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        var sale = SaleTestData.GenerateValidSale();
        var updatedSale = SaleTestData.GenerateValidSale();

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).GetByIdAsync(command.Id, Arg.Any<CancellationToken>());
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that validation is performed before repository calls.
    /// </summary>
    [Fact(DisplayName = "Given invalid command When handling Then validates before repository calls")]
    public async Task Handle_InvalidCommand_ValidatesBeforeRepositoryCalls()
    {
        // Given
        var command = new UpdateSaleCommand
        {
            Id = Guid.Empty // Invalid ID
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
        await _saleRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that update result contains correct information.
    /// </summary>
    [Fact(DisplayName = "Given successful update When handling Then returns correct result information")]
    public async Task Handle_SuccessfulUpdate_ReturnsCorrectResultInformation()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd = SalesTestData.GenerateAddSaleItemCommands(1);
        
        var sale = SaleTestData.GenerateValidSale();
        var updatedSale = SaleTestData.GenerateValidSaleWithItems(1);

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(updatedSale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Id.Should().Be(updatedSale.Id);
        result.SaleNumber.Should().Be(updatedSale.SaleNumber);
        result.TotalAmount.Should().Be(updatedSale.TotalAmount);
        result.Status.Should().Be(updatedSale.Status);
        result.ItemCount.Should().Be(updatedSale.Items.Count);
        result.UpdatedAt.Should().BeAfter(DateTime.MinValue);
        result.Message.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests adding items with invalid data throws validation exception.
    /// </summary>
    [Fact(DisplayName = "Given invalid items to add When updating sale Then throws validation exception")]
    public async Task Handle_InvalidItemsToAdd_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToAdd = new List<AddSaleItemCommand>
        {
            new AddSaleItemCommand
            {
                ProductId = Guid.Empty, // Invalid
                ProductName = "",       // Invalid
                UnitPrice = -1,         // Invalid
                Quantity = 0            // Invalid
            }
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests updating items with invalid data throws validation exception.
    /// </summary>
    [Fact(DisplayName = "Given invalid items to update When updating sale Then throws validation exception")]
    public async Task Handle_InvalidItemsToUpdate_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ItemsToUpdate = new List<UpdateSaleItemCommand>
        {
            new UpdateSaleItemCommand
            {
                ProductId = Guid.Empty, // Invalid
                Quantity = 0            // Invalid
            }
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests removing items with empty GUID throws validation exception.
    /// </summary>
    [Fact(DisplayName = "Given empty GUID to remove When updating sale Then throws validation exception")]
    public async Task Handle_EmptyGuidToRemove_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidUpdateSaleCommand();
        command.ProductIdsToRemove = new List<Guid> { Guid.Empty };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }
}