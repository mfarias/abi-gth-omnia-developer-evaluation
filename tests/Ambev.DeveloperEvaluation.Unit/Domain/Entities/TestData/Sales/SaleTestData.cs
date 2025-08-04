using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

public static class SaleTestData
{
    private static readonly Faker<Sale> SaleFaker = new Faker<Sale>()
        .RuleFor(s => s.Id, f => f.Random.Guid())
        .RuleFor(s => s.CustomerId, f => f.Random.Guid())
        .RuleFor(s => s.CustomerName, f => f.Person.FullName)
        .RuleFor(s => s.CustomerEmail, f => f.Internet.Email())
        .RuleFor(s => s.BranchId, f => f.Random.Guid())
        .RuleFor(s => s.BranchName, f => f.Company.CompanyName())
        .RuleFor(s => s.BranchLocation, f => f.Address.City())
        .RuleFor(s => s.SaleDate, f => f.Date.Recent(30))
        .RuleFor(s => s.Status, f => SaleStatus.Confirmed)
        .RuleFor(s => s.CreatedAt, f => f.Date.Recent(30))
        .RuleFor(s => s.UpdatedAt, f => f.Date.Recent(10));

    public static Sale GenerateValidSale()
    {
        return SaleFaker.Generate();
    }
    public static Sale GenerateValidSaleWithItems(int itemCount = 3)
    {
        var sale = GenerateValidSale();
        for (int i = 0; i < itemCount; i++)
        {
            var item = SaleItemTestData.GenerateValidSaleItem();
            sale.AddItem(item);
        }
        return sale;
    }

    public static Sale GenerateCancelledSale()
    {
        var sale = GenerateValidSale();
        sale.Cancel();
        return sale;
    }

    // Business rule: Maximum 20 items per product
    public static Sale GenerateSaleWithMaxItems()
    {
        var sale = GenerateValidSale();
        var item = SaleItemTestData.GenerateValidSaleItem();
        item.UpdateQuantity(20);
        sale.AddItem(item);
        return sale;
    }

    public static Guid GenerateValidCustomerId()
    {
        return new Faker().Random.Guid();
    }

    public static string GenerateValidCustomerName()
    {
        return new Faker().Person.FullName;
    }

    public static string GenerateValidCustomerEmail()
    {
        return new Faker().Internet.Email();
    }

    public static Guid GenerateValidBranchId()
    {
        return new Faker().Random.Guid();
    }

    public static string GenerateValidBranchName()
    {
        return new Faker().Company.CompanyName();
    }

    public static string GenerateValidBranchLocation()
    {
        return new Faker().Address.City();
    }

    public static Sale GenerateEmptySale()
    {
        var sale = GenerateValidSale();
        sale.Items.Clear();
        return sale;
    }
    public static Sale GenerateInvalidSale()
    {
        return new Sale
        {
            CustomerId = Guid.Empty,
            CustomerName = string.Empty,
            CustomerEmail = "invalid-email",
            BranchId = Guid.Empty,
            BranchName = string.Empty,
            BranchLocation = string.Empty
        };
    }
}