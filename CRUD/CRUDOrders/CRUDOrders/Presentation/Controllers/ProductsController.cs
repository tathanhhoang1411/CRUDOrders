using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CRUDOrders.Application.Commands;
using CRUDOrders.Application.Queries;
using CRUDOrders.Application.DTOs;

namespace CRUDOrders.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ApiExplorerSettings(GroupName = "products")]
[Tags("2. Phân hệ Quản lý Sản phẩm")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm phân trang có hỗ trợ tìm kiếm.
    /// </summary>
    /// <param name="skip">Số bản ghi bỏ qua (mặc định: 0).</param>
    /// <param name="take">Số bản ghi lấy (mặc định: 10).</param>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên hoặc mã sản phẩm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery
        {
            Skip = skip,
            Take = take,
            Keyword = keyword,
            CancellationToken = cancellationToken
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
