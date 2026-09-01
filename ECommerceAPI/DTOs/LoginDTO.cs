using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs;

public class LoginDTO
{
    [Required]
    [EmailAddress]
    [MaxLength(60)]
    public string? Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}