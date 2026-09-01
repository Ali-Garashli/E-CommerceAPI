using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs;

public class ProductCreationDTO
{
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(250)]
    public string? Description { get; set; }

    [Precision(7, 2)]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int CategoryId { get; set; }

    public bool IsActive { get; set; }
}

