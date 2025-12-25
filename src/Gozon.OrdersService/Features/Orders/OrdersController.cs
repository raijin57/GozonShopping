using Microsoft.AspNetCore.Mvc;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Features.Orders.Contracts;

namespace OrdersService.Features.Orders;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be positive");
        }

        var order = await orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, OrderResponse.From(order));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return OrderResponse.From(order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<OrderResponse>>> GetByUser([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        var orders = await orderService.GetByUserAsync(userId, cancellationToken);
        return orders.Select(OrderResponse.From).ToList();
    }
}

