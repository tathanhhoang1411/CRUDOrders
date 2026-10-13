using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CRUDOrders.Application.Commands;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Application.Queries;
using CRUDOrders.Infrastructure.Data;
using CRUDOrders.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CRUDOrders.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Custom WebApplicationFactory dung InMemory Database cho Integration Tests
// ─────────────────────────────────────────────────────────────────────────────
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Thay the ApplicationDbContext bang InMemory Database cho test co lap
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Integration Tests: Auth Endpoints
// ─────────────────────────────────────────────────────────────────────────────
public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkAndToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<LoginResponse>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.Username.Should().Be("admin");
        result.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<Result<object>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task RegisterAdmin_ShouldSucceed()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newadmin_" + Guid.NewGuid().ToString("N")[..6],
            Email = "newadmin_" + Guid.NewGuid().ToString("N")[..6] + "@example.com",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register-admin", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<JsonElement>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.GetProperty("role").GetString().Should().Be("Admin");
    }

    [Fact]
    public async Task RegisterStaff_WithoutAdminToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "staff_unauth",
            Email = "staff_unauth@example.com",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register-staff", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterStaff_WithAdminToken_ShouldSucceed()
    {
        // Arrange: login as admin first
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponse>>(JsonOptions);
        var token = loginResult!.Data!.AccessToken;

        var request = new RegisterRequest
        {
            Username = "newstaff_" + Guid.NewGuid().ToString("N")[..6],
            Email = "newstaff_" + Guid.NewGuid().ToString("N")[..6] + "@example.com",
            Password = "password123"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Auth/register-staff")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<JsonElement>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.GetProperty("role").GetString().Should().Be("Staff");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Integration Tests: Products Endpoints
// ─────────────────────────────────────────────────────────────────────────────
public class ProductsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProductsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        });
        var result = await loginRes.Content.ReadFromJsonAsync<Result<LoginResponse>>(JsonOptions);
        return result!.Data!.AccessToken;
    }

    [Fact]
    public async Task GetProducts_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/Products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithValidToken_ShouldReturnProductList()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/Products?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<ProductDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProducts_WithNegativeSkip_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act (skip = -1 < 0)
        var response = await _client.GetAsync("/api/v1/Products?skip=-1&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<ProductDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ErrorMessage.Should().Contain("skip");
    }

    [Fact]
    public async Task GetProducts_WithSkipExceedingMax_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act (skip = 100 khong duoc be hon 100)
        var response = await _client.GetAsync("/api/v1/Products?skip=100&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<ProductDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ErrorMessage.Should().Contain("100");
    }

    [Fact]
    public async Task GetProducts_WithTakeLessThanOne_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act (take = 0 < 1)
        var response = await _client.GetAsync("/api/v1/Products?skip=0&take=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<ProductDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ErrorMessage.Should().Contain("take");
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidCommand = new CreateProductCommand
        {
            Code = "",          // Loi: ma trong
            Name = "",          // Loi: ten trong
            Price = -5000,      // Loi: gia am
            Stock = -1          // Loi: ton kho am
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Products", invalidCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result<ProductDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueCode = "TEST_" + Guid.NewGuid().ToString("N")[..8];
        var command = new CreateProductCommand
        {
            Code = uniqueCode,
            Name = "Integration Test Product",
            Description = "Sample description",
            Price = 500000,
            Stock = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Products", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<Result<ProductDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Code.Should().Be(uniqueCode);
        result.Data.Price.Should().Be(500000);
        result.Data.Stock.Should().Be(20);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Integration Tests: Orders Endpoints
// ─────────────────────────────────────────────────────────────────────────────
public class OrdersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public OrdersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        });
        var result = await loginRes.Content.ReadFromJsonAsync<Result<LoginResponse>>(JsonOptions);
        return result!.Data!.AccessToken;
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidOrder = new CreateOrderCommand
        {
            OrderItems = new List<OrderItemInput>() // Loi: danh sach trong
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Orders", invalidOrder);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result<OrderDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentProduct_ShouldReturnProductNotFound()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var order = new CreateOrderCommand
        {
            OrderItems = new List<OrderItemInput>
            {
                new() { ProductId = 99999999L, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Orders", order);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result<OrderDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }
}
