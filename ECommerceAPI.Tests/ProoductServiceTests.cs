using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Tests;

public class ProoductServiceTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DataContext _dataContext;
    private readonly ProductService _productService;

    public ProoductServiceTests()
    {
        _sqliteConnection = new SqliteConnection("Filename=:memory:");
        _sqliteConnection.Open();

        DbContextOptions<DataContext> options =
            new DbContextOptionsBuilder<DataContext>().UseSqlite(_sqliteConnection).Options;

        _dataContext = new SqliteTestDataContext(options);
        // build schema on the temporary database
        _dataContext.Database.EnsureCreated();

        _productService = new ProductService(_dataContext);
    }

    // SQLite doesn't support EF Core decimals which are stored as text
    // Price is changed to double for this test
    private sealed class SqliteTestDataContext : DataContext
    {
        public SqliteTestDataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                        .Property(p => p.Price)
                        .HasConversion<double>();
        }
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _sqliteConnection.Dispose();
    }


    // GET ALL PRODUCTS
    [Fact]
    public async Task GetAllProductsAsync_WhenNoFiltersApplied_ShouldReturnAllProducts()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Alpha", category.Id);
        await SeedProductAsync("Beta", category.Id);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null);

        // Assert
        Assert.Equal(2, pagedProducts.TotalCount);
        Assert.Equal(2, pagedProducts.ProductResultItems.Count);
    }

    [Fact]
    public async Task GetAllProductsAsync_MatchingName_ShouldFilterBySearchTerm()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Wireless Mouse",
                               category.Id,
                               description: "Ergonomic");
        await SeedProductAsync("Mechanical Keyboard",
                               category.Id,
                               description: "RGB backlit");

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: "Mouse",
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null);

        // Assert
        Assert.Equal(1, pagedProducts.TotalCount);
        Assert.Equal("Wireless Mouse", pagedProducts.ProductResultItems.Single().Name);
    }

    [Fact]
    public async Task GetAllProductsAsync_MatchingDescription_ShouldFilterBySearchTerm()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Wireless Mouse",
                               category.Id,
                               description: "Ergonomic");
        await SeedProductAsync("Mechanical Keyboard",
                               category.Id,
                               description: "RGB backlit");

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: "RGB",
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null);

        // Assert
        Assert.Equal(1, pagedProducts.TotalCount);
        Assert.Equal("Mechanical Keyboard", pagedProducts.ProductResultItems.Single().Name);
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldFilterByPriceRange()
    {
        // Assign
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Cheap", category.Id, price: 10m);
        await SeedProductAsync("Mid", category.Id, price: 20m);
        await SeedProductAsync("MidHigh", category.Id, price: 30m);
        await SeedProductAsync("Expensive", category.Id, price: 40m);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: 15m,
                                                                                   maxPrice: 35m,
                                                                                   categoryId: null);

        // Assert
        Assert.Equal(2, pagedProducts.TotalCount);
        Assert.Equal(new[] { "Mid", "MidHigh" },
                     pagedProducts.ProductResultItems.Select(p => p.Name)
                                                     .OrderBy(n => n));
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldFilterByCategory()
    {
        // Assign
        Category electronics = await SeedCategoryAsync("Electronics");
        Category groceries = await SeedCategoryAsync("Groceries");
        await SeedProductAsync("Laptop", electronics.Id);
        await SeedProductAsync("Apple", groceries.Id);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: electronics.Id);

        // Assert
        Assert.Equal(1, pagedProducts.TotalCount);
        Assert.Equal("Laptop", pagedProducts.ProductResultItems.Single().Name);
    }

    [Theory]
    [InlineData(false, new[] { "Cheap", "Mid", "Expensive" })]
    [InlineData(true, new[] { "Expensive", "Mid", "Cheap" })]
    public async Task GetAllProductsAsync_ShouldSortByPrice(bool isDescending, string[] expectedOrder)
    {
        // Assign
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Cheap", category.Id, price: 10m);
        await SeedProductAsync("Mid", category.Id, price: 20m);
        await SeedProductAsync("Expensive", category.Id, price: 30m);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null,
                                                                                   sortBy: "price",
                                                                                   descendingSortOrder: isDescending);

        // Assert
        Assert.Equal(expectedOrder, pagedProducts.ProductResultItems.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task GetAllProductsAsync_WhenNoSortSpecified_ShouldDefaultToName()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Alpha", category.Id);
        await SeedProductAsync("Zeta", category.Id);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null);

        // Assert
        Assert.Equal(new[] { "Alpha", "Zeta" },
                     pagedProducts.ProductResultItems.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task GetAllProductsAsync_UnrecognizedSortBy_ShouldSortNameAscending()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Zeta", category.Id);
        await SeedProductAsync("Alpha", category.Id);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null,
                                                                                   sortBy: "made up field",
                                                                                   descendingSortOrder: false);

        // Assert
        Assert.Equal(new[] { "Zeta", "Alpha" },
                     pagedProducts.ProductResultItems.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldApplyPagination()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        for (int i = 1; i <= 25; i++)
            await SeedProductAsync($"Product {i}", category.Id);

        // Act
        PagedProductsDTO firstPage = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                               minPrice: null,
                                                                               maxPrice: null,
                                                                               categoryId: null,
                                                                               sortBy: "name",
                                                                               descendingSortOrder: false,
                                                                               page: 1,
                                                                               pageSize: 20);

        PagedProductsDTO secondPage = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                minPrice: null,
                                                                                maxPrice: null,
                                                                                categoryId: null,
                                                                                sortBy: "name",
                                                                                descendingSortOrder: false,
                                                                                page: 2,
                                                                                pageSize: 20);

        // Assert
        Assert.Equal(25, firstPage.TotalCount);
        Assert.Equal(20, firstPage.ProductResultItems.Count);
        Assert.Equal(25, secondPage.TotalCount);
        Assert.Equal(5, secondPage.ProductResultItems.Count);
        // check for no repetition in pages:
        Assert.Empty(firstPage.ProductResultItems.Select(p => p.Name)
                                                 .Intersect(secondPage.ProductResultItems.Select(p => p.Name)));
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldNormalizePageBelowOneTo1()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Test Product", category.Id);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null,
                                                                                   page: 0);

        // Assert
        Assert.Equal(1, pagedProducts.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task GetAllProductsAsync_ShouldNormalizeInvalidPageSizeTo20(int invalidPageSize)
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        await SeedProductAsync("Test Product", category.Id);

        // Act
        PagedProductsDTO pagedProducts = await _productService.GetAllProductsAsync(searchTerm: null,
                                                                                   minPrice: null,
                                                                                   maxPrice: null,
                                                                                   categoryId: null,
                                                                                   pageSize: invalidPageSize);
        // Assert
        Assert.Equal(20, pagedProducts.PageSize);
    }


    // GET PRODUCTS BY ID
    [Fact]
    public async Task GetProductByIdAsync_WhenProductExists_ShouldReturnIt()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        Product product = await SeedProductAsync("Test Product",
                                                 category.Id,
                                                 price: 42m);

        // Act
        ProductResultDTO? productResult = await _productService.GetProductByIdAsync(product.Id);

        // Assert
        Assert.NotNull(productResult);
        Assert.Equal("Test Product", productResult!.Name);
        Assert.Equal(42m, productResult.Price);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Act
        ProductResultDTO? productResult = await _productService.GetProductByIdAsync(id: 99);

        // Assert
        Assert.Null(productResult);
    }


    // PRODUCT CREATION
    [Fact]
    public async Task AddProductAsync_WhenCategoryDoesNotExist_ShouldThrowCategoryNotFoundException()
    {
        // Arrange
        ProductCreationDTO productDTO = new()
        {
            Name = "New Product",
            Price = 9.99m,
            Stock = 3,
            CategoryId = 9999,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _productService.AddProductAsync(productDTO));
    }

    [Fact]
    public async Task AddProductAsync_WhenCategoryExists_CreatesAndPersistsProduct()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        ProductCreationDTO productDTO = new()
        {
            Name = "New Product",
            Description = "This is a new product",
            Price = 9.99m,
            Stock = 3,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act
        ProductResultDTO productResult = await _productService.AddProductAsync(productDTO);

        // Assert
        Assert.True(productResult.Id > 0);
        Assert.Equal("New Product", productResult.Name);

        Product? persistedProduct = await _dataContext.Products.FindAsync(productResult.Id);
        Assert.NotNull(persistedProduct);
        Assert.Equal("This is a new product", persistedProduct!.Description);
        Assert.True(persistedProduct.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task AddProductAsync_WhenNullDescriptionProvided_ShouldDefaultToEmptyString()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        ProductCreationDTO productDTO = new()
        {
            Name = "No Description",
            Description = null,
            Price = 5m,
            Stock = 1,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act
        ProductResultDTO productResult = await _productService.AddProductAsync(productDTO);

        // Assert
        Product persistedProduct = await _dataContext.Products.FindAsync(productResult.Id)
                                   ?? throw new Exception("not persisted");
        Assert.Equal("", persistedProduct.Description);
    }


    // PRODUCT UPDATE
    [Fact]
    public async Task UpdateProductAsync_WhenProductDoesNotExist_ShouldThrowProductNotFoundException()
    {
        // Arrange
        // no products are seeded
        Category category = await SeedCategoryAsync();

        ProductUpdateDTO newProduct = new()
        {
            Id = 999,
            Name = "Doesn't matter",
            Price = 10m,
            Stock = 1,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _productService.UpdateProductAsync(999, newProduct));
    }

    [Fact]
    public async Task UpdateProductAsync_WhenNewCategoryDoesNotExist_ShouldThrowCategoryNotFoundException()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        Product product = await SeedProductAsync("Existing", category.Id);
        ProductUpdateDTO newProduct = new()
        {
            Id = product.Id,
            Name = "Existing",
            Price = 10m,
            Stock = 1,
            CategoryId = 9999,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _productService.UpdateProductAsync(product.Id, newProduct));
    }

    [Fact]
    public async Task UpdateProductAsync_WhenValid_ShouldUpdateFields()
    {
        // Arrange
        Category originalCategory = await SeedCategoryAsync("Electronics");
        Category newCategory = await SeedCategoryAsync("Healthcare");

        Product product = await SeedProductAsync("Old Name",
                                                 originalCategory.Id,
                                                 price: 10m,
                                                 stock: 5,
                                                 description: "Old description");

        ProductUpdateDTO newProduct = new()
        {
            Id = product.Id,
            Name = "New Name",
            Description = "New description",
            Price = 25m,
            Stock = 8,
            CategoryId = newCategory.Id,
            IsActive = false
        };

        // Act
        ProductResultDTO productResult = await _productService.UpdateProductAsync(product.Id, newProduct);

        // Assert
        Assert.Equal("New Name", productResult.Name);
        Assert.Equal("New description", productResult.Description);
        Assert.Equal(25m, productResult.Price);
        Assert.Equal(8, productResult.Stock);
        Assert.Equal(newCategory.Id, productResult.CategoryId);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenNewNameAndDescriptionAreBlank_ShouldKeepOriginals()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        Product product = await SeedProductAsync("Original Name",
                                                 category.Id,
                                                 description: "Original Description");

        ProductUpdateDTO newProduct = new()
        {
            Id = product.Id,
            Name = "   ",
            Price = 15m,
            Description = "",
            Stock = 2,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act
        ProductResultDTO result = await _productService.UpdateProductAsync(product.Id, newProduct);

        // Assert
        Assert.Equal("Original Name", result.Name);
        Assert.Equal("Original Description", result.Description);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldSetUpdatedAtTimestamp()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        Product product = await SeedProductAsync("Item", category.Id, updatedAt: null);

        ProductUpdateDTO newProduct = new()
        {
            Id = product.Id,
            Name = "Item",
            Price = 15m,
            Stock = 2,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act
        await _productService.UpdateProductAsync(product.Id, newProduct);

        // Assert
        Product persisted = await _dataContext.Products.FindAsync(product.Id)
                            ?? throw new Exception("not persisted");
        Assert.NotNull(persisted.UpdatedAt);
        Assert.True(persisted.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
    }


    // PRODUCT DELETION
    [Fact]
    public async Task DeleteProduct_WhenProductDoesNotExist_ShouldThrowProductNotFoundException()
    {
        // no product is seeded
        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _productService.DeleteProduct(999));
    }

    [Fact]
    public async Task DeleteProduct_WhenProductExists_ShouldRemoveIt()
    {
        // Arrange
        Category category = await SeedCategoryAsync();
        Product product = await SeedProductAsync("Doomed", category.Id);

        // Act
        await _productService.DeleteProduct(product.Id);

        // Assert
        Product? persisted = await _dataContext.Products.FindAsync(product.Id);
        Assert.Null(persisted);
    }


    // HELPERS
    private async Task<Category> SeedCategoryAsync(string name = "Electronics")
    {
        Category category = new() { Name = name };
        _dataContext.Categories.Add(category);
        await _dataContext.SaveChangesAsync();
        return category;
    }

    private async Task<Product> SeedProductAsync(string name,
                                                 int categoryId,
                                                 decimal price = 10m,
                                                 string? description = null,
                                                 int stock = 5,
                                                 bool isActive = true,
                                                 DateTime? createdAt = null,
                                                 DateTime? updatedAt = null)
    {
        Product product = new()
        {
            Name = name,
            Description = description ?? "",
            Price = price,
            Stock = stock,
            CategoryId = categoryId,
            IsActive = isActive,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt
        };

        _dataContext.Products.Add(product);
        await _dataContext.SaveChangesAsync();

        return product;
    }
}

