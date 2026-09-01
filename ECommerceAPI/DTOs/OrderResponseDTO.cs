using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs;

public class OrderResponseDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();

    public static OrderResponseDTO ConvertToDTO(Order order)
        => new()
           {
               Id = order.Id,
               UserId = order.UserId,
               Status = order.Status,
               TotalAmount = order.TotalAmount,
               CreatedAt = order.CreatedAt,
               Items = order.OrderItems.Select(OrderItemResponse.ConvertToDTO).ToList()
           };
}