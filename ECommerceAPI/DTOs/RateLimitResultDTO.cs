namespace ECommerceAPI.DTOs;

public class RateLimitResultDTO
{
    public bool Allowed { get; init; }

    public int Limit { get; init; }

    public int Remaining { get; init; }

    public DateTime WindowEnd { get; init; }
}

