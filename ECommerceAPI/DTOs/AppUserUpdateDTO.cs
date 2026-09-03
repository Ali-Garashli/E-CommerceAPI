using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs;

public class AppUserUpdateDTO
{
    [Key]
    public int Id { get; set; }

    [EmailAddress]
    [MaxLength(60)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? FirstName { get; set; }

    [MaxLength(20)]
    public string? LastName { get; set; }

    [Range(0, 150)]
    public int Age { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be between 8 and 100 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    public string? Password { get; set; }
}

