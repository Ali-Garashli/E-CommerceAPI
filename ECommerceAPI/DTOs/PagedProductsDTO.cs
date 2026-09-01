namespace ECommerceAPI.DTOs;

public class PagedProductsDTO
{
    public List<ProductResultDTO> ProductResultItems { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
