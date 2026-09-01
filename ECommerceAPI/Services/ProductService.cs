using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Services;

public class ProductService
{
    private readonly DataContext _dataContext;

    public ProductService(DataContext dataContext)
        => _dataContext = dataContext;

    // GET ALL
    public async Task<PagedProductsDTO> GetAllProductsAsync(
        [FromQuery] string? searchTerm,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? categoryId,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool descendingSortOrder = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0 || pageSize > 100)
            pageSize = 20;

        IQueryable<Product> query = _dataContext.Products;

        // filter by name
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(q => q.Name.Contains(searchTerm)
                                     || q.Description!.Contains(searchTerm));

        // filter by price
        if (minPrice is decimal minPrc)
            query = query.Where(q => q.Price >= minPrc);
        if (maxPrice is decimal maxPrc)
            query = query.Where(q => q.Price <= maxPrc);

        // filter by category
        if (categoryId is int ctgryId)
            query = query.Where(q => q.CategoryId == ctgryId);

        int totalCount = await query.CountAsync();

        // sort
        query = sortBy?.ToLower() switch
        {
            "name" => descendingSortOrder
                      ? query.OrderByDescending(q => q.Name)
                      : query.OrderBy(q => q.Name),

            "price" => descendingSortOrder
                       ? query.OrderByDescending(q => q.Price)
                       : query.OrderBy(q => q.Price),

            "createdat" => descendingSortOrder
                           ? query.OrderByDescending(q => q.CreatedAt)
                           : query.OrderBy(q => q.CreatedAt),

            "updatedat" => descendingSortOrder
                           ? query.OrderByDescending(q => q.UpdatedAt)
                           : query.OrderBy(q => q.UpdatedAt),

            _ => query.OrderBy(q => q.Name)
        };

        // apply pagination
        query = query.Skip((page - 1) * pageSize)
                     .Take(pageSize);

        return new PagedProductsDTO
        {
            ProductResultItems = await query.Select(q => ConvertToDTO(q)).ToListAsync(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // GET
    public async Task<ProductResultDTO?> GetProductByIdAsync(int id)
    {
        Product? product = await _dataContext.Products.FindAsync(id);
        return product is null
               ? null
               : ConvertToDTO(product);
    }

    // POST
    public async Task<ProductResultDTO> AddProductAsync(ProductCreationDTO productDTO)
    {
        if (!await CategoryExistsAsync(productDTO.CategoryId))
            throw new CategoryNotFoundException(productDTO.CategoryId);

        Product newProduct = new()
        {
            Name = productDTO.Name,
            Description = productDTO.Description ?? "",
            Price = productDTO.Price,
            Stock = productDTO.Stock,
            CategoryId = productDTO.CategoryId,
            CreatedAt = DateTime.UtcNow,
            IsActive = productDTO.IsActive
        };

        await _dataContext.Products.AddAsync(newProduct);
        await _dataContext.SaveChangesAsync();

        return ConvertToDTO(newProduct);
    }

    // PUT
    public async Task<ProductResultDTO> UpdateProductAsync(int id, ProductUpdateDTO productDTO)
    {
        Product? product = await _dataContext.Products.FindAsync(id)
                           ?? throw new ProductNotFoundException(id);

        if (!await CategoryExistsAsync(productDTO.CategoryId))
            throw new CategoryNotFoundException(productDTO.CategoryId);

        if (!string.IsNullOrWhiteSpace(productDTO.Name))
            product.Name = productDTO.Name;

        if (!string.IsNullOrWhiteSpace(productDTO.Description))
            product.Description = productDTO.Description;

        product.Price = productDTO.Price;
        product.Stock = productDTO.Stock;
        product.CategoryId = productDTO.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;
        product.IsActive = productDTO.IsActive;

        await _dataContext.SaveChangesAsync();

        return ConvertToDTO(product);
    }


    // DELETE
    public async Task DeleteProduct(int id)
    {
        Product? product = await _dataContext.Products.FindAsync(id)
                           ?? throw new ProductNotFoundException(id);

        _dataContext.Products.Remove(product);
        await _dataContext.SaveChangesAsync();
    }

    // HELPERS
    private async Task<bool> CategoryExistsAsync(int categoryId)
    => await _dataContext.Categories.AnyAsync(c => c.Id == categoryId);

    public static ProductResultDTO ConvertToDTO(Product product)
        => new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId
        };
}

