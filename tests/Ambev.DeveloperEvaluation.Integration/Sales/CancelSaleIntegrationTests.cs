using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using AutoMapper;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class CancelSaleIntegrationTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly SaleRepository _repository;
    private readonly CancelSaleHandler _handler;
    private readonly ILogger<CancelSaleHandler> _logger;

    public CancelSaleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultContext(options);
        _repository = new SaleRepository(_context);
        _logger = Substitute.For<ILogger<CancelSaleHandler>>();
        _handler = new CancelSaleHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_ValidSale_ShouldCancelSaleInDatabase()
    {
        var sale = await CreateSaleInDatabase();
        var command = new CancelSaleCommand { Id = sale.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        var cancelledSale = await _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == sale.Id);
        cancelledSale?.Status.Should().Be(SaleStatus.Cancelled);
        cancelledSale?.Items.Should().AllSatisfy(item => item.IsCancelled.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_AlreadyCancelledSale_ShouldThrowException()
    {
        var sale = await CreateCancelledSaleInDatabase();
        var command = new CancelSaleCommand { Id = sale.Id };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already cancelled");
    }

    [Fact]
    public async Task Handle_NonExistentSale_ShouldThrowException()
    {
        var command = new CancelSaleCommand { Id = Guid.NewGuid() };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found");
    }

    [Fact]
    public async Task Handle_SaleWithMultipleItems_ShouldCancelAllItems()
    {
        var sale = await CreateSaleWithMultipleItemsInDatabase();
        var command = new CancelSaleCommand { Id = sale.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        var cancelledSale = await _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == sale.Id);
        cancelledSale?.Items.Should().HaveCount(3);
        cancelledSale?.Items.Should().AllSatisfy(item => item.IsCancelled.Should().BeTrue());
        cancelledSale?.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CancelSale_ShouldSetCancellationDate()
    {
        var sale = await CreateSaleInDatabase();
        var beforeCancellation = DateTime.UtcNow;
        var command = new CancelSaleCommand { Id = sale.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.CancelledAt.Should().BeAfter(beforeCancellation);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CancelSale_ShouldPersistChangesToDatabase()
    {
        var sale = await CreateSaleInDatabase();
        var command = new CancelSaleCommand { Id = sale.Id };

        await _handler.Handle(command, CancellationToken.None);

        var persistedSale = await _context.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sale.Id);

        persistedSale?.Status.Should().Be(SaleStatus.Cancelled);
        persistedSale?.Items.Should().AllSatisfy(item => item.IsCancelled.Should().BeTrue());
    }

    private async Task<Sale> CreateSaleInDatabase()
    {
        var faker = new Faker();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = faker.Random.AlphaNumeric(10),
            CustomerId = faker.Random.Guid(),
            CustomerName = faker.Name.FullName(),
            CustomerEmail = faker.Internet.Email(),
            BranchId = faker.Random.Guid(),
            BranchName = faker.Company.CompanyName(),
            BranchLocation = faker.Address.FullAddress(),
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Confirmed
        };

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = faker.Random.Guid(),
            ProductName = faker.Commerce.ProductName(),
            ProductSku = faker.Commerce.Ean13(),
            UnitPrice = faker.Random.Decimal(1, 100),
            Quantity = faker.Random.Int(1, 5),
            DiscountPercentage = 0
        };

        sale.Items.Add(item);


        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        return sale;
    }

    private async Task<Sale> CreateSaleWithMultipleItemsInDatabase()
    {
        var faker = new Faker();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = faker.Random.AlphaNumeric(10),
            CustomerId = faker.Random.Guid(),
            CustomerName = faker.Name.FullName(),
            CustomerEmail = faker.Internet.Email(),
            BranchId = faker.Random.Guid(),
            BranchName = faker.Company.CompanyName(),
            BranchLocation = faker.Address.FullAddress(),
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Confirmed
        };

        for (int i = 0; i < 3; i++)
        {
            var item = new SaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                ProductId = faker.Random.Guid(),
                ProductName = faker.Commerce.ProductName(),
                ProductSku = faker.Commerce.Ean13(),
                UnitPrice = faker.Random.Decimal(1, 100),
                Quantity = faker.Random.Int(1, 5),
                DiscountPercentage = 0
            };
            sale.Items.Add(item);
        }



        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        return sale;
    }

    private async Task<Sale> CreateCancelledSaleInDatabase()
    {
        var sale = await CreateSaleInDatabase();
        sale.Cancel();
        await _context.SaveChangesAsync();
        return sale;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}