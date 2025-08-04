using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
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

public class GetSaleIntegrationTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly SaleRepository _repository;
    private readonly GetSaleHandler _handler;
    private readonly IMapper _mapper;

    public GetSaleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DefaultContext(options);
        _repository = new SaleRepository(_context);
        _mapper = Substitute.For<IMapper>();
        _handler = new GetSaleHandler(_repository, _mapper);

        SetupMapper();
    }

    [Fact]
    public async Task Handle_ExistingSale_ShouldReturnSaleFromDatabase()
    {
        var sale = await CreateSaleInDatabase();
        var command = new GetSaleCommand { Id = sale.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(sale.Id);
        result.CustomerId.Should().Be(sale.CustomerId);
        result.BranchId.Should().Be(sale.BranchId);
        result.Items.Should().HaveCount(sale.Items.Count);
    }

    [Fact]
    public async Task Handle_NonExistentSale_ShouldThrowArgumentException()
    {
        var command = new GetSaleCommand { Id = Guid.NewGuid() };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Sale not found");
    }

    [Fact]
    public async Task Handle_SaleWithItems_ShouldIncludeAllItemDetails()
    {
        var sale = await CreateSaleWithMultipleItemsInDatabase();
        var command = new GetSaleCommand { Id = sale.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item =>
        {
            item.ProductId.Should().NotBeEmpty();
            item.ProductName.Should().NotBeNullOrEmpty();
            item.UnitPrice.Should().BeGreaterThan(0);
            item.Quantity.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task Handle_CancelledSale_ShouldReturnCancelledStatus()
    {
        var sale = await CreateCancelledSaleInDatabase();
        var command = new GetSaleCommand { Id = sale.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(SaleStatus.Cancelled);
        result.Items.Should().AllSatisfy(item => item.IsCancelled.Should().BeTrue());
    }

    private async Task<Sale> CreateSaleInDatabase()
    {
        var faker = new Faker();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = faker.Random.AlphaNumeric(10),
            CustomerId = faker.Random.Guid(),
            BranchId = faker.Random.Guid(),
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Confirmed
        };

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = faker.Random.Guid(),
            ProductName = faker.Commerce.ProductName(),
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
            BranchId = faker.Random.Guid(),
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Confirmed
        };

        var item1 = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = faker.Random.Guid(),
            ProductName = faker.Commerce.ProductName(),
            UnitPrice = faker.Random.Decimal(1, 100),
            Quantity = 6,
            DiscountPercentage = 10m
        };

        var item2 = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = faker.Random.Guid(),
            ProductName = faker.Commerce.ProductName(),
            UnitPrice = faker.Random.Decimal(1, 100),
            Quantity = 12,
            DiscountPercentage = 20m
        };

        sale.Items.Add(item1);
        sale.Items.Add(item2);


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

    private void SetupMapper()
    {
        _mapper.Map<GetSaleResult>(Arg.Any<Sale>()).Returns(callInfo =>
        {
            var sale = callInfo.Arg<Sale>();
            return new GetSaleResult
            {
                Id = sale.Id,
                SaleNumber = sale.SaleNumber,
                CustomerId = sale.CustomerId,
                BranchId = sale.BranchId,
                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount,
                Status = sale.Status,
                Items = sale.Items.Select(i =>                 
                new GetSaleItemResult
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    DiscountPercentage = i.DiscountPercentage,
                    DiscountAmount = i.DiscountAmount,
                    TotalAmount = i.TotalAmount,
                    IsCancelled = i.IsCancelled
                }).ToList()
            };
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}