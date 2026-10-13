using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using CRUDOrders.Application.Commands;
using CRUDOrders.Application.Validators;
using CRUDOrders.Application.Behaviors;
using CRUDOrders.Application.Handlers;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Application.Queries;
using CRUDOrders.Core.Entities;
using CRUDOrders.Infrastructure.Data.Repositories;
using CRUDOrders.Infrastructure.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using AutoMapper;
using MediatR;

namespace CRUDOrders.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// CreateProductCommandHandler Tests
// ─────────────────────────────────────────────────────────────────────────────

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<CreateProductCommandHandler>> _mockLogger;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockIdGenerator = new Mock<IIdGenerator>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<CreateProductCommandHandler>>();

        _handler = new CreateProductCommandHandler(
            _mockProductRepository.Object,
            _mockIdGenerator.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateProduct()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Code = "TEST001",
            Name = "Test Product",
            Price = 100m,
            Stock = 10
        };

        var expectedId = 12345L;
        var productDto = new ProductDto
        {
            Id = expectedId,
            Code = command.Code,
            Name = command.Name,
            Price = command.Price,
            Stock = command.Stock
        };

        _mockProductRepository.Setup(r => r.GetByCodeAsync(command.Code, default))
            .ReturnsAsync((Product?)null);

        _mockIdGenerator.Setup(g => g.NewId()).Returns(expectedId);

        _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>(), default))
            .ReturnsAsync(new Product { Id = expectedId, Code = command.Code, Name = command.Name });

        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns(productDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data?.Id.Should().Be(expectedId);
        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateCode_ShouldReturnError()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Code = "EXISTING",
            Name = "Test Product",
            Price = 100m,
            Stock = 10
        };

        var existingProduct = new Product { Id = 1, Code = "EXISTING", Name = "Existing" };
        _mockProductRepository.Setup(r => r.GetByCodeAsync(command.Code, default))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_ALREADY_EXISTS");
        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Never);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ValidationBehavior Tests
// ─────────────────────────────────────────────────────────────────────────────

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithValidationErrors_ShouldReturnErrorResult_WithoutCallingNext()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ValidationBehavior<CreateProductCommand, Result<ProductDto>>>>();
        var validator = new CreateProductCommandValidator();
        var behavior = new ValidationBehavior<CreateProductCommand, Result<ProductDto>>(
            new[] { validator }, mockLogger.Object);

        var command = new CreateProductCommand
        {
            Code = "",        // Invalid: empty
            Name = "",        // Invalid: empty
            Price = -1m,      // Invalid: negative
            Stock = -5        // Invalid: negative
        };

        var nextCalled = false;
        RequestHandlerDelegate<Result<ProductDto>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result<ProductDto>.Ok(new ProductDto()));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeFalse("handler should NOT be called when validation fails");
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallNext()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ValidationBehavior<CreateProductCommand, Result<ProductDto>>>>();
        var validator = new CreateProductCommandValidator();
        var behavior = new ValidationBehavior<CreateProductCommand, Result<ProductDto>>(
            new[] { validator }, mockLogger.Object);

        var command = new CreateProductCommand
        {
            Code = "VALID001",
            Name = "Valid Product",
            Price = 100m,
            Stock = 10
        };

        var nextCalled = false;
        var expectedResult = Result<ProductDto>.Ok(new ProductDto { Id = 1 });
        RequestHandlerDelegate<Result<ProductDto>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue("handler SHOULD be called when validation passes");
        result.Should().Be(expectedResult);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CreateOrderCommandHandler Tests (legacy approach without ApplicationDbContext)
// ─────────────────────────────────────────────────────────────────────────────

public class CreateOrderInsufficientStockTests
{
    [Fact]
    public async Task CreateOrderValidator_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            UserId = 1,
            OrderItems = new List<OrderItemInput>() // Empty list
        };

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("it nhat mot san pham") || e.PropertyName == "OrderItems");
    }

    [Fact]
    public async Task CreateOrderValidator_WithInvalidQuantity_ShouldFail()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            UserId = 1,
            OrderItems = new List<OrderItemInput>
            {
                new OrderItemInput { ProductId = 1, Quantity = 0 } // Invalid: 0 quantity
            }
        };

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public async Task CreateOrderValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            UserId = 1,
            OrderItems = new List<OrderItemInput>
            {
                new OrderItemInput { ProductId = 1, Quantity = 5 }
            }
        };

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Result<T> Tests
// ─────────────────────────────────────────────────────────────────────────────

public class ResultTests
{
    [Fact]
    public void Ok_ShouldReturnSuccessResult()
    {
        var data = new ProductDto { Id = 1, Name = "Test" };
        var result = Result<ProductDto>.Ok(data);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(data);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Error_ShouldReturnFailedResult()
    {
        var result = Result<ProductDto>.Error("NOT_FOUND", "San pham khong ton tai");

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorCode.Should().Be("NOT_FOUND");
        result.ErrorMessage.Should().Be("San pham khong ton tai");
    }
}
