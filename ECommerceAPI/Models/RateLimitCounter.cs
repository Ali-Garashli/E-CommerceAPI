namespace ECommerceAPI.Models;

public class RateLimitCounter
{
    public int Id { get; set; }

    public string PolicyName { get; set; } = null!;

    public string Client { get; set; } = null!;

    public DateTime WindowStart { get; set; }

    public int RequestCount { get; set; }
}

