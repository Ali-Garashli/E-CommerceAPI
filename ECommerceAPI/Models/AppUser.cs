using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class AppUser
{
    [Key]
    public int Id { get; set; }

    [MaxLength(30)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [Range(0, 150)]
    public int Age { get; set; }

    public string Role { get; set; } = "Customer";
}