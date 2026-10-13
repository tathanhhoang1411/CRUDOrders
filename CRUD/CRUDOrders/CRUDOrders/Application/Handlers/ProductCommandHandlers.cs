using AutoMapper;
using MediatR;
using CRUDOrders.Application.Commands;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Application.Common;
using CRUDOrders.Core.Entities;
using CRUDOrders.Infrastructure.Data.Repositories;
using CRUDOrders.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace CRUDOrders.Application.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IIdGenerator idGenerator,
        IMapper mapper,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _idGenerator = idGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Validation duoc xu ly boi ValidationBehavior trong MediatR pipeline

        var existingProduct = await _productRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existingProduct != null)
        {
            _logger.LogWarning("Product with code {Code} already exists", request.Code);
            return Result<ProductDto>.Error("PRODUCT_ALREADY_EXISTS", "Ma san pham da ton tai");
        }

        var product = new Product
        {
            Id = _idGenerator.NewId(),
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTimeHelper.UtcPlus7Now
        };

        await _productRepository.AddAsync(product, cancellationToken);
        _logger.LogInformation("Product {ProductId} ({Code}) created successfully", product.Id, product.Code);

        return Result<ProductDto>.Ok(_mapper.Map<ProductDto>(product));
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // Validation duoc xu ly boi ValidationBehavior trong MediatR pipeline

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", request.Id);
            return Result<ProductDto>.Error("PRODUCT_NOT_FOUND", "San pham khong ton tai");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;

        await _productRepository.UpdateAsync(product, cancellationToken);
        _logger.LogInformation("Product {ProductId} updated successfully", product.Id);

        return Result<ProductDto>.Ok(_mapper.Map<ProductDto>(product));
    }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for delete", request.Id);
            return Result<bool>.Error("PRODUCT_NOT_FOUND", "San pham khong ton tai");
        }

        await _productRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Product {ProductId} deleted successfully", request.Id);

        return Result<bool>.Ok(true);
    }
}
