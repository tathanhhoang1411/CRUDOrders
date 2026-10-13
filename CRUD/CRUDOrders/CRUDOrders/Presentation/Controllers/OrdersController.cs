using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CRUDOrders.Application.Commands;
using CRUDOrders.Application.Queries;
using CRUDOrders.Application.DTOs;
using System.Security.Claims;

namespace CRUDOrders.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ApiExplorerSettings(GroupName = "orders")]
[Tags("3. Phân hệ Quản lý Đơn hàng")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy toàn bộ danh sách đơn hàng phân trang (Dành cho Admin).
    /// </summary>
    /// <param name="skip">Số bản ghi bỏ qua (mặc định: 0).</param>
    /// <param name="take">Số bản ghi lấy (mặc định: 10).</param>
    /// <param name="keyword">Từ khóa tìm kiếm theo trạng thái đơn hàng.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOrdersQuery
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

    /// <summary>
    /// Lấy danh sách đơn hàng của người dùng hiện tại có phân trang.
    /// </summary>
    /// <param name="skip">Số bản ghi bỏ qua (mặc định: 0).</param>
    /// <param name="take">Số bản ghi lấy (mặc định: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("user/my-orders")]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(Result<object>.Error("INVALID_USER", "Không xác định được người dùng"));

        var query = new GetUserOrdersQuery
        {
            UserId = userId,
            Skip = skip,
            Take = take,
            CancellationToken = cancellationToken
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        // Get userId from claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(Result<object>.Error("INVALID_USER", "Không xác định được người dùng"));

        command.UserId = userId;
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateOrderStatusCommand command,
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
        var command = new DeleteOrderCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
