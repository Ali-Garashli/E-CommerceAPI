namespace ECommerceAPI.Models;

public class RateLimitPolicy
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public bool Enabled { get; set; } = true;
}