namespace ECommerceAPI.DTOs;

public class OrderCreateDTO
{
    public List<OrderItemCreateDTO> OrderItemDTOs { get; set; } = new();
}