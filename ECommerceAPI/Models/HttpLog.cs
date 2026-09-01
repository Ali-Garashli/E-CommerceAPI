using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class HttpLog
{
    [Key]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // request
    [MaxLength(7)]
    public string Method { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;

    // response
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

