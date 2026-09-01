namespace ECommerceAPI.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal ProductPrice { get; set; }
    public decimal TotalPrice { get => ProductPrice * Quantity; }
}

