using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using AutoMapper;
using Bogus;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class CreateSaleIntegrationTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly SaleRepository _repository;
    private readonly CreateSaleHandler _handler;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultContext(options);
        _repository = new SaleRepository(_context);
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<CreateSaleHandler>>();
        _handler = new CreateSaleHandler(_repository, _mapper, _logger);

        SetupMapper();
    }

    [Fact]
    public async Task Handle_ValidSale_ShouldPersistToDatabase()
    {
        var command = GenerateValidCreateSaleCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        var savedSale = await _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == result.Id);
        savedSale.Should().NotBeNull();
        savedSale?.CustomerId.Should().Be(command.CustomerId);
        savedSale?.BranchId.Should().Be(command.BranchId);
        savedSale?.Items.Should().HaveCount(command.Items.Count);
    }

    [Fact]
    public async Task Handle_SaleWithMultipleItems_ShouldCalculateDiscountsCorrectly()
    {
        var command = GenerateCreateSaleCommandWithDiscountItems();

        var result = await _handler.Handle(command, CancellationToken.None);

        var savedSale = await _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == result.Id);
        var itemWith10Discount = savedSale?.Items.First(i => i.Quantity >= 4 && i.Quantity < 10);
        var itemWith20Discount = savedSale?.Items.First(i => i.Quantity >= 10);

        itemWith10Discount?.DiscountPercentage.Should().Be(10m);
        itemWith20Discount?.DiscountPercentage.Should().Be(20m);
    }

    [Fact]
    public async Task Handle_SaleWithBusinessRuleViolation_ShouldThrowException()
    {
        var command = GenerateCreateSaleCommandWithTooManyItems();

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_MultipleSales_ShouldGenerateUniqueSaleNumbers()
    {
        var command1 = GenerateValidCreateSaleCommand();
        var command2 = GenerateValidCreateSaleCommand();

        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        var sale1 = await _context.Sales.FindAsync(result1.Id);
        var sale2 = await _context.Sales.FindAsync(result2.Id);

        sale1?.SaleNumber.Should().NotBe(sale2?.SaleNumber);
    }

    private CreateSaleCommand GenerateValidCreateSaleCommand()
    {
        var faker = new Faker();
        return new CreateSaleCommand
        {
            CustomerId = faker.Random.Guid(),
            CustomerName = faker.Person.FullName,
            CustomerEmail = faker.Internet.Email(),
            BranchId = faker.Random.Guid(),
            BranchName = faker.Company.CompanyName(),
            Items = new List<CreateSaleItemCommand>
            {
                new CreateSaleItemCommand
                {
                    ProductId = faker.Random.Guid(),
                    ProductName = faker.Commerce.ProductName(),
                    ProductSku = faker.Commerce.Ean13(),
                    UnitPrice = faker.Random.Decimal(1, 100),
                    Quantity = faker.Random.Int(1, 3)
                }
            }
        };
    }

    private CreateSaleCommand GenerateCreateSaleCommandWithDiscountItems()
    {
        var faker = new Faker();
        return new CreateSaleCommand
        {
            CustomerId = faker.Random.Guid(),
            CustomerName = faker.Person.FullName,
            CustomerEmail = faker.Internet.Email(),
            BranchId = faker.Random.Guid(),
            BranchName = faker.Company.CompanyName(),
            Items = new List<CreateSaleItemCommand>
            {
                new CreateSaleItemCommand
                {
                    ProductId = faker.Random.Guid(),
                    ProductName = faker.Commerce.ProductName(),
                    ProductSku = faker.Commerce.Ean13(),
                    UnitPrice = faker.Random.Decimal(1, 100),
                    Quantity = 6
                },
                new CreateSaleItemCommand
                {
                    ProductId = faker.Random.Guid(),
                    ProductName = faker.Commerce.ProductName(),
                    ProductSku = faker.Commerce.Ean13(),
                    UnitPrice = faker.Random.Decimal(1, 100),
                    Quantity = 15
                }
            }
        };
    }

    private CreateSaleCommand GenerateCreateSaleCommandWithTooManyItems()
    {
        var faker = new Faker();
        return new CreateSaleCommand
        {
            CustomerId = faker.Random.Guid(),
            CustomerName = faker.Person.FullName,
            CustomerEmail = faker.Internet.Email(),
            BranchId = faker.Random.Guid(),
            BranchName = faker.Company.CompanyName(),
            Items = new List<CreateSaleItemCommand>
            {
                new CreateSaleItemCommand
                {
                    ProductId = faker.Random.Guid(),
                    ProductName = faker.Commerce.ProductName(),
                    ProductSku = faker.Commerce.Ean13(),
                    UnitPrice = faker.Random.Decimal(1, 100),
                    Quantity = 25
                }
            }
        };
    }

    private void SetupMapper()
    {
        _mapper.Map<CreateSaleResult>(Arg.Any<Sale>()).Returns(callInfo =>
        {
            var sale = callInfo.Arg<Sale>();
            return new CreateSaleResult
            {
                Id = sale.Id,
                SaleNumber = sale.SaleNumber,
                CustomerId = sale.CustomerId,
                BranchId = sale.BranchId,
                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount,
                Status = sale.Status
            };
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}