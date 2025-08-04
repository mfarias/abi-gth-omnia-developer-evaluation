using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

public static class SaleItemTestData
{

    public static SaleItem GenerateValidSaleItem()
    {
        var faker = new Faker();
        return new SaleItem(
            faker.Random.Guid(),
            faker.Commerce.ProductName(),
            faker.Commerce.Ean13(),
            faker.Random.Decimal(1, 1000),
            faker.Random.Int(1, 5)
        );
    }

    // Business rule: 10% discount for 4-9 items
    public static SaleItem GenerateSaleItemWith10PercentDiscount()
    {
        var faker = new Faker();
        return new SaleItem(
            faker.Random.Guid(),
            faker.Commerce.ProductName(),
            faker.Commerce.Ean13(),
            faker.Random.Decimal(10, 100),
            faker.Random.Int(4, 9) // 10% discount range
        );
    }

    // Business rule: 20% discount for 10-20 items
    public static SaleItem GenerateSaleItemWith20PercentDiscount()
    {
        var faker = new Faker();
        return new SaleItem(
            faker.Random.Guid(),
            faker.Commerce.ProductName(),
            faker.Commerce.Ean13(),
            faker.Random.Decimal(10, 100),
            faker.Random.Int(10, 20) // 20% discount range
        );
    }

    // Business rule: No discount for 1-3 items
    public static SaleItem GenerateSaleItemWithNoDiscount()
    {
        var faker = new Faker();
        return new SaleItem(
            faker.Random.Guid(),
            faker.Commerce.ProductName(),
            faker.Commerce.Ean13(),
            faker.Random.Decimal(10, 100),
            faker.Random.Int(1, 3) // No discount range
        );
    }

    // Business rule: Maximum 20 items per product
    public static SaleItem GenerateSaleItemWithMaxQuantity()
    {
        var faker = new Faker();
        return new SaleItem(
            faker.Random.Guid(),
            faker.Commerce.ProductName(),
            faker.Commerce.Ean13(),
            faker.Random.Decimal(10, 100),
            20 // Maximum allowed quantity
        );
    }


    public static SaleItem GenerateCancelledSaleItem()
    {
        var item = GenerateValidSaleItem();
        item.Cancel();
        return item;
    }


    public static Guid GenerateValidProductId()
    {
        return new Faker().Random.Guid();
    }


    public static string GenerateValidProductName()
    {
        return new Faker().Commerce.ProductName();
    }


    public static string GenerateValidProductSku()
    {
        return new Faker().Commerce.Ean13();
    }

    public static decimal GenerateValidUnitPrice()
    {
        return new Faker().Random.Decimal(1, 1000);
    }


    public static int GenerateValidQuantity()
    {
        return new Faker().Random.Int(1, 5);
    }


    public static SaleItem GenerateInvalidSaleItem()
    {
        return new SaleItem(
            Guid.Empty,
            string.Empty,
            string.Empty,
            -1, // Invalid negative price
            -1  // Invalid negative quantity
        );
    }
}