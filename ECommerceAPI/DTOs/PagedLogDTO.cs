using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs;

public class PagedLogDTO
{
    public List<HttpLog> LogItems { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}