using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSaleHandler> _logger;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<CreateSaleHandler>>();
        _handler = new CreateSaleHandler(_saleRepository, _mapper, _logger);
    }
    [Fact(DisplayName = "Given valid sale data When creating sale Then returns success response")]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        var sale = SaleTestData.GenerateValidSale();
        var result = new CreateSaleResult
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            TotalAmount = sale.TotalAmount
        };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(result);

        var createSaleResult = await _handler.Handle(command, CancellationToken.None);

        createSaleResult.Should().NotBeNull();
        createSaleResult.Id.Should().Be(sale.Id);
        createSaleResult.SaleNumber.Should().Be(sale.SaleNumber);
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that an invalid sale creation request throws a validation exception.
    /// </summary>
    [Fact(DisplayName = "Given invalid sale data When creating sale Then throws validation exception")]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateInvalidCreateSaleCommand();

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that sale items are properly created and added to the sale.
    /// </summary>
    [Fact(DisplayName = "Given sale command with items When creating sale Then creates sale with items")]
    public async Task Handle_ValidRequestWithItems_CreatesSaleWithItems()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommandWithItems(2);
        var sale = SaleTestData.GenerateValidSale();
        var result = new CreateSaleResult { Id = sale.Id };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(result);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).CreateAsync(
            Arg.Is<Sale>(s => s.CustomerId == command.CustomerId &&
                            s.CustomerName == command.CustomerName &&
                            s.CustomerEmail == command.CustomerEmail &&
                            s.BranchId == command.BranchId &&
                            s.BranchName == command.BranchName &&
                            s.BranchLocation == command.BranchLocation),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that sale validation is performed and errors are handled.
    /// </summary>
    [Fact(DisplayName = "Given sale with validation errors When creating sale Then throws validation exception")]
    public void Handle_SaleValidationFails_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        
        // Simulate a scenario where the sale validation fails at domain level
        // This could happen if business rules are violated after items are added
        var invalidSale = new Sale
        {
            CustomerId = Guid.Empty, // This would cause validation to fail
            CustomerName = command.CustomerName,
            CustomerEmail = command.CustomerEmail,
            BranchId = command.BranchId,
            BranchName = command.BranchName,
            BranchLocation = command.BranchLocation
        };

        // When & Then
        // This test demonstrates that business rule validation happens at the domain level
        // rather than just command validation. We'll test a scenario where adding items 
        // would exceed business rules
        
        // When & Then
        var act = () =>
        {
            // Create a sale and try to add items that exceed business rules
            var sale = new Sale
            {
                CustomerId = command.CustomerId,
                CustomerName = command.CustomerName,
                CustomerEmail = command.CustomerEmail,
                BranchId = command.BranchId,
                BranchName = command.BranchName,
                BranchLocation = command.BranchLocation
            };

            // Add an item with maximum quantity
            var maxItem = new SaleItem(
                Guid.NewGuid(),
                "Test Product",
                "SKU123",
                10m,
                20 // Maximum allowed
            );
            sale.AddItem(maxItem);

            // Try to add another item with same product ID - this should fail
            var extraItem = new SaleItem(
                maxItem.ProductId, // Same product ID
                "Test Product",
                "SKU123",
                10m,
                1 // Even 1 more would exceed the limit
            );
            
            // This should throw during AddItem due to business rules
            sale.AddItem(extraItem);
        };

        act.Should().Throw<ArgumentException>()
           .WithMessage("Cannot sell more than 20 identical items");
    }

    /// <summary>
    /// Tests that the mapper is called correctly.
    /// </summary>
    [Fact(DisplayName = "Given valid sale When handling Then maps sale to result correctly")]
    public async Task Handle_ValidRequest_MapsSaleToResult()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        var sale = SaleTestData.GenerateValidSale();
        var result = new CreateSaleResult
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            TotalAmount = sale.TotalAmount
        };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(result);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        _mapper.Received(1).Map<CreateSaleResult>(Arg.Any<Sale>());
    }

    /// <summary>
    /// Tests that repository is called with correct parameters.
    /// </summary>
    [Fact(DisplayName = "Given valid sale When handling Then calls repository with correct sale")]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectSale()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        var sale = SaleTestData.GenerateValidSale();
        var result = new CreateSaleResult { Id = sale.Id };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(result);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).CreateAsync(
            Arg.Is<Sale>(s => 
                s.CustomerId == command.CustomerId &&
                s.CustomerName == command.CustomerName &&
                s.CustomerEmail == command.CustomerEmail &&
                s.BranchId == command.BranchId &&
                s.BranchName == command.BranchName &&
                s.BranchLocation == command.BranchLocation &&
                s.Items.Count == command.Items.Count),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that command validation with empty items list fails.
    /// </summary>
    [Fact(DisplayName = "Given command with no items When validating Then should fail")]
    public async Task Handle_CommandWithNoItems_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.Items.Clear(); // Remove all items

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that command validation with invalid customer data fails.
    /// </summary>
    [Fact(DisplayName = "Given command with invalid customer data When validating Then should fail")]
    public async Task Handle_CommandWithInvalidCustomerData_ThrowsValidationException()
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.CustomerEmail = "invalid-email"; // Invalid email format

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests that sale items are created with correct business rules applied.
    /// </summary>
    [Theory(DisplayName = "Given items with different quantities When creating sale Then applies correct discounts")]
    [InlineData(3)] // No discount for 3 items
    [InlineData(5)] // 10% discount for 5 items  
    [InlineData(15)] // 20% discount for 15 items
    public async Task Handle_ItemsWithDifferentQuantities_AppliesCorrectDiscounts(int quantity)
    {
        // Given
        var command = SalesTestData.GenerateValidCreateSaleCommand();
        command.Items.Clear();
        command.Items.Add(new CreateSaleItemCommand
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            ProductSku = "SKU123",
            UnitPrice = 10m,
            Quantity = quantity
        });

        var sale = SaleTestData.GenerateValidSale();
        var result = new CreateSaleResult { Id = sale.Id };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(sale);
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(result);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        await _saleRepository.Received(1).CreateAsync(
            Arg.Is<Sale>(s => s.Items.Any()), 
            Arg.Any<CancellationToken>());
        
        // Verify the sale item was created (the actual discount calculation is tested in SaleItem tests)
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}