using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
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
/// Contains unit tests for the <see cref="CancelSaleHandler"/> class.
/// </summary>
public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILogger<CancelSaleHandler> _logger;
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _logger = Substitute.For<ILogger<CancelSaleHandler>>();
        _handler = new CancelSaleHandler(_saleRepository, _logger);
    }

    /// <summary>
    /// Tests that a valid cancel sale request is handled successfully.
    /// </summary>
    [Fact(DisplayName = "Given valid cancel request When cancelling sale Then returns success response")]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        var sale = SaleTestData.GenerateValidSaleWithItems(2);
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SaleId.Should().Be(sale.Id);
        result.SaleNumber.Should().Be(sale.SaleNumber);
        result.Message.Should().Contain($"Sale {sale.SaleNumber} has been successfully cancelled");
        result.CancelledAt.Should().BeAfter(DateTime.MinValue);
        await _saleRepository.Received(1).GetByIdAsync(command.Id, Arg.Any<CancellationToken>());
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that an invalid cancel sale request throws a validation exception.
    /// </summary>
    [Fact(DisplayName = "Given invalid cancel data When cancelling sale Then throws validation exception")]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateInvalidCancelSaleCommand();

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that cancelling a non-existent sale throws KeyNotFoundException.
    /// </summary>
    [Fact(DisplayName = "Given non-existent sale ID When cancelling sale Then throws KeyNotFoundException")]
    public async Task Handle_NonExistentSale_ThrowsKeyNotFoundException()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {command.Id} not found");
    }

    /// <summary>
    /// Tests that cancelling an already cancelled sale throws InvalidOperationException.
    /// </summary>
    [Fact(DisplayName = "Given already cancelled sale When cancelling sale Then throws InvalidOperationException")]
    public async Task Handle_AlreadyCancelledSale_ThrowsInvalidOperationException()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        var cancelledSale = SaleTestData.GenerateCancelledSale();
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(cancelledSale);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Sale {cancelledSale.SaleNumber} is already cancelled");
    }

    /// <summary>
    /// Tests that the sale status is updated to cancelled.
    /// </summary>
    [Fact(DisplayName = "Given valid sale When cancelling Then updates sale status to cancelled")]
    public async Task Handle_ValidSale_UpdatesSaleStatusToCancelled()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        sale.Status = SaleStatus.Confirmed; // Ensure it's confirmed initially
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.UpdatedAt.Should().NotBeNull();
        await _saleRepository.Received(1).UpdateAsync(
            Arg.Is<Sale>(s => s.Status == SaleStatus.Cancelled), 
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that repository is called with correct parameters.
    /// </summary>
    [Fact(DisplayName = "Given valid command When handling Then calls repository methods correctly")]
    public async Task Handle_ValidRequest_CallsRepositoryCorrectly()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        var sale = SaleTestData.GenerateValidSale();

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);

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
        var command = new CancelSaleCommand
        {
            Id = Guid.Empty, // Invalid ID
            CancellationReason = "" // Invalid reason
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
        await _saleRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that command constructor works correctly.
    /// </summary>
    [Fact(DisplayName = "Given sale ID and reason When creating command Then sets properties correctly")]
    public async Task Handle_CommandWithConstructor_SetsPropertiesCorrectly()
    {
        // Given
        var saleId = Guid.NewGuid();
        var reason = "Test cancellation reason";
        var command = new CancelSaleCommand(saleId, reason);
        var sale = SaleTestData.GenerateValidSale();
        
        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        command.Id.Should().Be(saleId);
        command.CancellationReason.Should().Be(reason);
        result.SaleId.Should().Be(sale.Id);
        await _saleRepository.Received(1).GetByIdAsync(saleId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that default cancellation reason is used when not specified.
    /// </summary>
    [Fact(DisplayName = "Given command without reason When creating Then uses default reason")]
    public void Handle_CommandWithoutReason_UsesDefaultReason()
    {
        // Given & When
        var command = new CancelSaleCommand();

        // Then
        command.CancellationReason.Should().Be("Sale cancelled by user");
    }

    /// <summary>
    /// Tests that command validation works correctly.
    /// </summary>
    [Theory(DisplayName = "Given different invalid inputs When validating Then should fail appropriately")]
    [InlineData("00000000-0000-0000-0000-000000000000", "Valid reason")] // Empty GUID
    [InlineData("550e8400-e29b-41d4-a716-446655440000", "")] // Empty reason
    [InlineData("00000000-0000-0000-0000-000000000000", "")] // Both invalid
    public async Task Handle_InvalidInputs_ThrowsValidationException(string guidString, string reason)
    {
        // Given
        var command = new CancelSaleCommand
        {
            Id = Guid.Parse(guidString),
            CancellationReason = reason
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that cancellation reason length validation works.
    /// </summary>
    [Fact(DisplayName = "Given very long cancellation reason When validating Then should fail")]
    public async Task Handle_VeryLongCancellationReason_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        command.CancellationReason = new string('x', 501); // Exceeds 500 character limit

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that the result contains accurate cancellation timestamp.
    /// </summary>
    [Fact(DisplayName = "Given successful cancellation When handling Then result contains accurate timestamp")]
    public async Task Handle_SuccessfulCancellation_ResultContainsAccurateTimestamp()
    {
        // Given
        var command = SalesTestData.GenerateValidCancelSaleCommand();
        var sale = SaleTestData.GenerateValidSale();
        var beforeCancellation = DateTime.UtcNow;
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);
        var afterCancellation = DateTime.UtcNow;

        // Then
        result.CancelledAt.Should().BeAfter(beforeCancellation);
        result.CancelledAt.Should().BeBefore(afterCancellation);
    }
}