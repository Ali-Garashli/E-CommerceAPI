using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
