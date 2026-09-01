using ECommerceAPI.Attributes;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductController(ProductService productService)
        => _productService = productService;

    // GET ALL
    [HttpGet]
    [RateLimitPolicy("ProductReadPolicy")]
    public async Task<ActionResult<PagedProductsDTO>> GetAllProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? categoryId,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool descendingSortOrder = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _productService.GetAllProductsAsync(searchTerm,
                                                        minPrice,
                                                        maxPrice,
                                                        categoryId,
                                                        sortBy,
                                                        descendingSortOrder,
                                                        page,
                                                        pageSize));

    // GET
    [HttpGet("{id}")]
    [RateLimitPolicy("ProductReadPolicy")]
    public async Task<ActionResult<ProductResultDTO>> GetProduct(int id)
    {
        ProductResultDTO? product = await _productService.GetProductByIdAsync(id);
        return product is null
               ? NotFound()
               : Ok(product);
    }

    // POST
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RateLimitPolicy("ProductWritePolicy")]
    public async Task<ActionResult<ProductResultDTO>> AddProduct(ProductCreationDTO productDTO)
    {
        ProductResultDTO product = await _productService.AddProductAsync(productDTO);
        return CreatedAtAction(nameof(GetProduct),
                               new { id = product.Id },
                               product);
    }

    // PUT
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [RateLimitPolicy("ProductWritePolicy")]
    public async Task<ActionResult<ProductResultDTO>> EditProduct(int id, ProductUpdateDTO productDTO)
    {
        if (productDTO.Id != id)
            return BadRequest();

        ProductResultDTO product = await _productService.UpdateProductAsync(id, productDTO);
        return Ok(product);
    }

    // DELETE
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [RateLimitPolicy("ProductWritePolicy")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteProduct(id);
        return NoContent();
    }
}

