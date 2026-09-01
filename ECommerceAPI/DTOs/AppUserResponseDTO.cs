namespace ECommerceAPI.DTOs;

public class AppUserResponseDTO
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Role { get; set; } = string.Empty;
}