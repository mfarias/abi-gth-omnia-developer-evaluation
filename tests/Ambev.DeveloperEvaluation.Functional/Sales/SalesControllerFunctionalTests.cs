using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

public class SalesControllerFunctionalTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly DefaultContext _context;

    public SalesControllerFunctionalTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DefaultContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<DefaultContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<DefaultContext>();
    }

    [Fact]
    public async Task CreateSale_ValidRequest_Returns201Created()
    {
        var request = GenerateValidCreateSaleRequest();

        var response = await _client.PostAsJsonAsync("/api/sales", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWithData<CreateSaleResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result?.Success.Should().BeTrue();
        result?.Data?.Id.Should().NotBeEmpty();
        result?.Data?.SaleNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateSale_InvalidRequest_Returns400BadRequest()
    {
        var request = new CreateSaleRequest
        {
            CustomerId = Guid.Empty,
            BranchId = Guid.Empty,
            Items = new List<CreateSaleItemRequest>()
        };

        var response = await _client.PostAsJsonAsync("/api/sales", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSale_ExistingSale_Returns200Ok()
    {
        var sale = await CreateSaleInDatabase();

        var response = await _client.GetAsync($"/api/sales/{sale.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWithData<GetSaleResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result?.Success.Should().BeTrue();
        result?.Data?.Id.Should().Be(sale.Id);
        result?.Data?.Items.Should().HaveCount(sale.Items.Count);
    }

    [Fact]
    public async Task GetSale_NonExistentSale_Returns404NotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/sales/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSale_ValidRequest_Returns200Ok()
    {
        var sale = await CreateSaleInDatabase();
        var request = new UpdateSaleRequest
        {
            ItemsToAdd = new List<AddSaleItemRequest>
            {
                new AddSaleItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "New Product",
                    UnitPrice = 50m,
                    Quantity = 2
                }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/sales/{sale.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWithData<UpdateSaleResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result?.Success.Should().BeTrue();
        result?.Data?.Id.Should().Be(sale.Id);
    }

    [Fact]
    public async Task UpdateSale_NonExistentSale_Returns404NotFound()
    {
        var request = new UpdateSaleRequest
        {
            ItemsToAdd = new List<AddSaleItemRequest>
            {
                new AddSaleItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "New Product",
                    UnitPrice = 50m,
                    Quantity = 2
                }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/sales/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelSale_ValidRequest_Returns200Ok()
    {
        var sale = await CreateSaleInDatabase();
        var request = new CancelSaleRequest();

        var response = await _client.PostAsJsonAsync($"/api/sales/{sale.Id}/cancel", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWithData<CancelSaleResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result?.Success.Should().BeTrue();
        result?.Data?.SaleId.Should().Be(sale.Id);
        result?.Data?.SaleId.Should().Be(sale.Id);
    }

    [Fact]
    public async Task CancelSale_NonExistentSale_Returns404NotFound()
    {
        var request = new CancelSaleRequest();

        var response = await _client.PostAsJsonAsync($"/api/sales/{Guid.NewGuid()}/cancel", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelSale_AlreadyCancelledSale_Returns400BadRequest()
    {
        var sale = await CreateCancelledSaleInDatabase();
        var request = new CancelSaleRequest();

        var response = await _client.PostAsJsonAsync($"/api/sales/{sale.Id}/cancel", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSale_WithDiscountItems_ShouldCalculateDiscountsCorrectly()
    {
        var request = new CreateSaleRequest
        {
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            Items = new List<CreateSaleItemRequest>
            {
                new CreateSaleItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product with 10% discount",
                    UnitPrice = 10m,
                    Quantity = 6
                },
                new CreateSaleItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product with 20% discount",
                    UnitPrice = 20m,
                    Quantity = 15
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWithData<CreateSaleResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var createdSale = await _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == result.Data.Id);
        var discountItem1 = createdSale?.Items.First(i => i.Quantity == 6);
        var discountItem2 = createdSale?.Items.First(i => i.Quantity == 15);
        
        discountItem1?.DiscountPercentage.Should().Be(10m);
        discountItem2?.DiscountPercentage.Should().Be(20m);
    }

    private CreateSaleRequest GenerateValidCreateSaleRequest()
    {
        var faker = new Faker();
        return new CreateSaleRequest
        {
            CustomerId = faker.Random.Guid(),
            BranchId = faker.Random.Guid(),
            Items = new List<CreateSaleItemRequest>
            {
                new CreateSaleItemRequest
                {
                    ProductId = faker.Random.Guid(),
                    ProductName = faker.Commerce.ProductName(),
                    UnitPrice = faker.Random.Decimal(1, 100),
                    Quantity = faker.Random.Int(1, 5)
                }
            }
        };
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

    private async Task<Sale> CreateCancelledSaleInDatabase()
    {
        var sale = await CreateSaleInDatabase();
        sale.Cancel();
        await _context.SaveChangesAsync();
        return sale;
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}