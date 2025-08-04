using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Contains unit tests for the <see cref="GetSaleHandler"/> class.
/// </summary>
public class GetSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly GetSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleHandlerTests"/> class.
    /// Sets up the test dependencies and creates fake data generators.
    /// </summary>
    public GetSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetSaleHandler(_saleRepository, _mapper);
    }

    /// <summary>
    /// Tests that a valid get sale request returns the sale successfully.
    /// </summary>
    [Fact(DisplayName = "Given valid sale ID When getting sale Then returns sale data")]
    public async Task Handle_ValidRequest_ReturnsSaleData()
    {
        // Given
        var command = SalesTestData.GenerateValidGetSaleCommand();
        var sale = SaleTestData.GenerateValidSaleWithItems(2);
        var result = new GetSaleResult
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            TotalAmount = sale.TotalAmount,
            Status = sale.Status,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerName,
            CustomerEmail = sale.CustomerEmail
        };

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<GetSaleResult>(sale).Returns(result);

        // When
        var getSaleResult = await _handler.Handle(command, CancellationToken.None);

        // Then
        getSaleResult.Should().NotBeNull();
        getSaleResult.Id.Should().Be(sale.Id);
        getSaleResult.SaleNumber.Should().Be(sale.SaleNumber);
        getSaleResult.TotalAmount.Should().Be(sale.TotalAmount);
        await _saleRepository.Received(1).GetByIdAsync(command.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that an invalid get sale request throws a validation exception.
    /// </summary>
    [Fact(DisplayName = "Given invalid sale ID When getting sale Then throws validation exception")]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateInvalidGetSaleCommand();

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that getting a non-existent sale throws KeyNotFoundException.
    /// </summary>
    [Fact(DisplayName = "Given non-existent sale ID When getting sale Then throws KeyNotFoundException")]
    public async Task Handle_NonExistentSale_ThrowsKeyNotFoundException()
    {
        // Given
        var command = SalesTestData.GenerateValidGetSaleCommand();
        
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {command.Id} not found");
    }

    /// <summary>
    /// Tests that the mapper is called correctly.
    /// </summary>
    [Fact(DisplayName = "Given valid sale When handling Then maps sale to result correctly")]
    public async Task Handle_ValidRequest_MapsSaleToResult()
    {
        // Given
        var command = SalesTestData.GenerateValidGetSaleCommand();
        var sale = SaleTestData.GenerateValidSaleWithItems(1);
        var result = new GetSaleResult { Id = sale.Id };

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<GetSaleResult>(sale).Returns(result);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        _mapper.Received(1).Map<GetSaleResult>(sale);
    }

    /// <summary>
    /// Tests that repository is called with correct parameters.
    /// </summary>
    [Fact(DisplayName = "Given valid command When handling Then calls repository with correct ID")]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectId()
    {
        // Given
        var command = SalesTestData.GenerateValidGetSaleCommand();
        var sale = SaleTestData.GenerateValidSale();
        var result = new GetSaleResult { Id = sale.Id };

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<GetSaleResult>(sale).Returns(result);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).GetByIdAsync(command.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that getting a cancelled sale returns the sale with correct status.
    /// </summary>
    [Fact(DisplayName = "Given cancelled sale ID When getting sale Then returns cancelled sale")]
    public async Task Handle_CancelledSale_ReturnsCancelledSale()
    {
        // Given
        var command = SalesTestData.GenerateValidGetSaleCommand();
        var sale = SaleTestData.GenerateCancelledSale();
        var result = new GetSaleResult
        {
            Id = sale.Id,
            Status = sale.Status
        };

        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<GetSaleResult>(sale).Returns(result);

        // When
        var getSaleResult = await _handler.Handle(command, CancellationToken.None);

        // Then
        getSaleResult.Should().NotBeNull();
        getSaleResult.Status.Should().Be(Ambev.DeveloperEvaluation.Domain.Enums.SaleStatus.Cancelled);
    }

    /// <summary>
    /// Tests that command validation works correctly with empty GUID.
    /// </summary>
    [Fact(DisplayName = "Given empty GUID When validating command Then should fail")]
    public async Task Handle_EmptyGuidCommand_ThrowsValidationException()
    {
        // Given
        var command = new GetSaleCommand
        {
            Id = Guid.Empty
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that command constructor works correctly.
    /// </summary>
    [Fact(DisplayName = "Given sale ID When creating command Then sets ID correctly")]
    public async Task Handle_CommandWithConstructor_SetsIdCorrectly()
    {
        // Given
        var saleId = Guid.NewGuid();
        var command = new GetSaleCommand(saleId);
        var sale = SaleTestData.GenerateValidSale();
        sale.Id = saleId;
        var result = new GetSaleResult { Id = sale.Id };

        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<GetSaleResult>(sale).Returns(result);

        // When
        var getSaleResult = await _handler.Handle(command, CancellationToken.None);

        // Then
        command.Id.Should().Be(saleId);
        getSaleResult.Id.Should().Be(saleId);
        await _saleRepository.Received(1).GetByIdAsync(saleId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that validation is performed before repository call.
    /// </summary>
    [Fact(DisplayName = "Given invalid command When handling Then validates before repository call")]
    public async Task Handle_InvalidCommand_ValidatesBeforeRepositoryCall()
    {
        // Given
        var command = new GetSaleCommand
        {
            Id = Guid.Empty
        };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
        await _saleRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}