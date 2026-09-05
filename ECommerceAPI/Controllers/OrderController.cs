using System.Security.Claims;
using ECommerceAPI.Attributes;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;

    private int CurrentUserId
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id)
           ? id
           : 0;
    private bool IsAdmin => User.IsInRole("Admin");

    public OrderController(OrderService orderService)
        => _orderService = orderService;

    // GET ALL
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<Order>>> GetAllOrders()
        => Ok(await _orderService.GetAllOrdersAsync());

    // GET
    [HttpGet("{id}")]
    [RateLimitPolicy("OrderReadPolicy")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        OrderResponseDTO? order = await _orderService.GetOrderByIdAsync(id,
                                                                        CurrentUserId,
                                                                        IsAdmin);
        return order is null
               ? NotFound()
               : Ok(order);
    }

    // POST
    [HttpPost]
    [RateLimitPolicy("OrderWritePolicy")]
    public async Task<ActionResult<OrderResponseDTO>> AddOrder(
        [FromBody] OrderCreateDTO orderCreateDTO,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        OrderResponseDTO order = await _orderService.CreateOrderAsync(CurrentUserId,
                                                                      orderCreateDTO,
                                                                      idempotencyKey);

        return CreatedAtAction(nameof(GetOrder),
                               new { id = order.Id },
                               order);
    }

    // PATCH
    [HttpPatch("{id}/status")]
    [RateLimitPolicy("OrderPatchPolicy")]
    public async Task<ActionResult<OrderResponseDTO>> UpdateOrderStatus(int id,
                                                                        OrderUpdateDTO request)
        => Ok(await _orderService.UpdateOrderStatusAsync(id,
                                                         CurrentUserId,
                                                         IsAdmin,
                                                         request.NewStatus));
}

