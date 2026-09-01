namespace ECommerceAPI.DTOs;

public class RateLimitUpdateDTO
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public bool Enabled { get; set; }
}

