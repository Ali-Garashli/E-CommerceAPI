namespace ECommerceAPI.Models;

public class IdempotencyKey
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public IdempotencyKeyStatus Status { get; set; } = IdempotencyKeyStatus.InProgress;

    public string? ResponseBody { get; set; } // for replaying a repeat call

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
