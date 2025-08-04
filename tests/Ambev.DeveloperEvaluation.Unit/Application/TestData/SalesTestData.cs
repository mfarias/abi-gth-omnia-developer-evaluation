using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Bogus;
using System.Linq;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

public static class SalesTestData
{

    private static readonly Faker<CreateSaleCommand> CreateSaleCommandFaker = new Faker<CreateSaleCommand>()
        .RuleFor(c => c.CustomerId, f => f.Random.Guid())
        .RuleFor(c => c.CustomerName, f => f.Person.FullName)
        .RuleFor(c => c.CustomerEmail, f => f.Internet.Email())
        .RuleFor(c => c.BranchId, f => f.Random.Guid())
        .RuleFor(c => c.BranchName, f => f.Company.CompanyName())
        .RuleFor(c => c.BranchLocation, f => f.Address.City())
        .RuleFor(c => c.Items, f => GenerateCreateSaleItemCommands(f.Random.Int(1, 3)));


    private static readonly Faker<CreateSaleItemCommand> CreateSaleItemCommandFaker = new Faker<CreateSaleItemCommand>()
        .RuleFor(i => i.ProductId, f => f.Random.Guid())
        .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
        .RuleFor(i => i.ProductSku, f => f.Commerce.Ean13())
        .RuleFor(i => i.UnitPrice, f => f.Random.Decimal(1, 1000))
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 20));


    private static readonly Faker<GetSaleCommand> GetSaleCommandFaker = new Faker<GetSaleCommand>()
        .RuleFor(c => c.Id, f => f.Random.Guid());


    private static readonly Faker<CancelSaleCommand> CancelSaleCommandFaker = new Faker<CancelSaleCommand>()
        .RuleFor(c => c.Id, f => f.Random.Guid())
        .RuleFor(c => c.CancellationReason, f => f.Lorem.Sentence());


    private static readonly Faker<AddSaleItemCommand> AddSaleItemCommandFaker = new Faker<AddSaleItemCommand>()
        .RuleFor(i => i.ProductId, f => f.Random.Guid())
        .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
        .RuleFor(i => i.ProductSku, f => f.Commerce.Ean13())
        .RuleFor(i => i.UnitPrice, f => f.Random.Decimal(1, 1000))
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 20));


    private static readonly Faker<UpdateSaleItemCommand> UpdateSaleItemCommandFaker = new Faker<UpdateSaleItemCommand>()
        .RuleFor(i => i.ProductId, f => f.Random.Guid())
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 20));


    public static CreateSaleCommand GenerateValidCreateSaleCommand()
    {
        return CreateSaleCommandFaker.Generate();
    }


    public static CreateSaleCommand GenerateValidCreateSaleCommandWithItems(int itemCount)
    {
        var command = CreateSaleCommandFaker.Generate();
        command.Items = GenerateCreateSaleItemCommands(itemCount);
        return command;
    }


    public static CreateSaleItemCommand GenerateValidCreateSaleItemCommand()
    {
        return CreateSaleItemCommandFaker.Generate();
    }


    public static List<CreateSaleItemCommand> GenerateCreateSaleItemCommands(int count)
    {
        return CreateSaleItemCommandFaker.Generate(count);
    }

    public static GetSaleCommand GenerateValidGetSaleCommand()
    {
        return GetSaleCommandFaker.Generate();
    }


    public static GetSaleCommand GenerateGetSaleCommand(Guid id)
    {
        return new GetSaleCommand(id);
    }


    public static CancelSaleCommand GenerateValidCancelSaleCommand()
    {
        return CancelSaleCommandFaker.Generate();
    }


    public static CancelSaleCommand GenerateCancelSaleCommand(Guid id, string reason = "Test cancellation")
    {
        return new CancelSaleCommand(id, reason);
    }


    public static UpdateSaleCommand GenerateValidUpdateSaleCommand()
    {
        var faker = new Faker();
        return new UpdateSaleCommand
        {
            Id = faker.Random.Guid(),
            ItemsToAdd = AddSaleItemCommandFaker.Generate(faker.Random.Int(0, 2)),
            ItemsToUpdate = UpdateSaleItemCommandFaker.Generate(faker.Random.Int(0, 2)),
            ProductIdsToRemove = Enumerable.Range(0, faker.Random.Int(0, 2))
                                      .Select(_ => faker.Random.Guid())
                                      .ToList()
        };
    }


    public static AddSaleItemCommand GenerateValidAddSaleItemCommand()
    {
        return AddSaleItemCommandFaker.Generate();
    }


    public static List<AddSaleItemCommand> GenerateAddSaleItemCommands(int count)
    {
        return AddSaleItemCommandFaker.Generate(count);
    }


    public static UpdateSaleItemCommand GenerateValidUpdateSaleItemCommand()
    {
        return UpdateSaleItemCommandFaker.Generate();
    }


    public static List<UpdateSaleItemCommand> GenerateUpdateSaleItemCommands(int count)
    {
        return UpdateSaleItemCommandFaker.Generate(count);
    }


    public static CreateSaleCommand GenerateInvalidCreateSaleCommand()
    {
        return new CreateSaleCommand
        {
            CustomerId = Guid.Empty,
            CustomerName = string.Empty,
            CustomerEmail = "invalid-email",
            BranchId = Guid.Empty,
            BranchName = string.Empty,
            BranchLocation = new string('x', 600), // Exceeds max length
            Items = new List<CreateSaleItemCommand>() // Empty items list
        };
    }


    public static CreateSaleItemCommand GenerateInvalidCreateSaleItemCommand()
    {
        return new CreateSaleItemCommand
        {
            ProductId = Guid.Empty,
            ProductName = string.Empty,
            ProductSku = string.Empty,
            UnitPrice = -1, // Invalid negative price
            Quantity = 0 // Invalid zero quantity
        };
    }


    public static GetSaleCommand GenerateInvalidGetSaleCommand()
    {
        return new GetSaleCommand
        {
            Id = Guid.Empty
        };
    }


    public static CancelSaleCommand GenerateInvalidCancelSaleCommand()
    {
        return new CancelSaleCommand
        {
            Id = Guid.Empty,
            CancellationReason = string.Empty
        };
    }


    public static UpdateSaleCommand GenerateInvalidUpdateSaleCommand()
    {
        return new UpdateSaleCommand
        {
            Id = Guid.Empty,
            ItemsToAdd = new List<AddSaleItemCommand>
            {
                new AddSaleItemCommand
                {
                    ProductId = Guid.Empty,
                    ProductName = string.Empty,
                    UnitPrice = -1,
                    Quantity = 0
                }
            },
            ProductIdsToRemove = new List<Guid> { Guid.Empty }
        };
    }
}