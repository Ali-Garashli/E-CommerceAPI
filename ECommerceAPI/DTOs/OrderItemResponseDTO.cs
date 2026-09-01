using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs;

public class OrderItemResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }

    public static OrderItemResponse ConvertToDTO(OrderItem item)
        => new()
           {
               ProductId = item.ProductId,
               ProductName = item.Product?.Name ?? string.Empty,
               Quantity = item.Quantity,
               UnitPrice = item.ProductPrice,
               Subtotal = item.TotalPrice
           };
}
