using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DataContext _dataContext;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _sqliteConnection = new SqliteConnection("Filename=:memory:");
        _sqliteConnection.Open();

        DbContextOptions<DataContext> options =
            new DbContextOptionsBuilder<DataContext>().UseSqlite(_sqliteConnection).Options;

        _dataContext = new DataContext(options);
        // build schema on the temporary database
        _dataContext.Database.EnsureCreated();

        _orderService = new OrderService(_dataContext);
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _sqliteConnection.Dispose();
    }

    // ORDER CREATION
    [Fact]
    public async Task CreateOrderAsync_ValidOrder_ShouldReduceStockAndReturnCorrectTotal()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync(stock: 10, price: 25.00m);
        OrderCreateDTO orderRequest = new()
        {
            OrderItemDTOs = new List<OrderItemCreateDTO>
            {
                new() {
                    ProductId = product.Id,
                    Quantity = 3
                }
            }
        };

        // Act
        OrderResponseDTO orderResponse =
            await _orderService.CreateOrderAsync(user.Id, orderRequest);

        // Assert
        Assert.Equal(75.00m, orderResponse.TotalAmount); // 3 * 25.00 = 75.00
        Assert.Equal(OrderStatus.Pending, orderResponse.Status);
        Assert.Single(orderResponse.Items);

        // check if status update is correct
        Product? updatedProduct = await _dataContext.Products.FindAsync(product.Id);
        Assert.Equal(7, updatedProduct!.Stock); // 10 - 3 = 7
    }

    [Fact]
    public async Task CreateOrderAsync_MultipleProducts_ShouldSumTotalCorrectly()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product productA = await SeedProductAsync(stock: 5, price: 10.00m);
        Product productB = await SeedProductAsync(stock: 5, price: 15.00m);
        OrderCreateDTO orderRequest = new()
        {
            OrderItemDTOs = new List<OrderItemCreateDTO>
            {
                new() {
                    ProductId = productA.Id,
                    Quantity = 2 // total: 20.00
                },
                new() {
                    ProductId = productB.Id,
                    Quantity = 1 // total: 15.00
                }  
            }
        };

        // Act
        OrderResponseDTO orderResponse =
            await _orderService.CreateOrderAsync(user.Id, orderRequest);

        // Assert
        Assert.Equal(35.00m, orderResponse.TotalAmount);
        Assert.Equal(2, orderResponse.Items.Count);
    }

    // STOCK VALIDATION
    [Fact]
    public async Task CreateOrderAsync_WhenInsufficientStock_ShouldThrowInsufficientStockException()
    {
        // Arrange
        Product product = await SeedProductAsync(stock: 2);
        OrderCreateDTO orderRequest = new()
        {
            OrderItemDTOs = new List<OrderItemCreateDTO>
            {
                new() {
                    ProductId = product.Id,
                    Quantity = 5 // quantity more than stock
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _orderService.CreateOrderAsync(1, orderRequest));

        // updated product should keep its stock as 2 after rollback
        Product? updatedProduct = await _dataContext.Products.FindAsync(product.Id);
        Assert.Equal(2, updatedProduct!.Stock);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenProductDoesNotExist_ShouldThrowProductNotFoundException()
    {
        // Arrange
        AppUser user = await SeedUserAsync();

        // make an order with no product seeded
        OrderCreateDTO orderRequest = new()
        {
            OrderItemDTOs = new List<OrderItemCreateDTO>
            {
                new() {
                    ProductId = 123,
                    Quantity = 1
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _orderService.CreateOrderAsync(user.Id, orderRequest));
    }

    [Fact]
    public async Task CreateOrderAsync_InactiveProduct_ShouldThrowProductNotFoundException()
    {
        // Arrange
        AppUser user = await SeedUserAsync();

        // product exists, but is deactivated
        Product product = await SeedProductAsync(stock: 10, isActive: false);
        OrderCreateDTO orderRequest = new()
        {
            OrderItemDTOs = new List<OrderItemCreateDTO>
            {
                new() {
                    ProductId = product.Id,
                    Quantity = 1
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _orderService.CreateOrderAsync(user.Id, orderRequest));
    }

    // STATUS TRANSITIONS
    [Fact]
    public async Task UpdateOrderStatusAsync_WhenPendingToConfirmedByAdmin_ShouldSucceed()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync();
        OrderResponseDTO order = await OrderOneProductForUser(user.Id, product);

        // Act
        OrderResponseDTO updatedOrder = await _orderService.UpdateOrderStatusAsync(order.Id,
                                                                                   requesterId: 99,
                                                                                   isAdmin: true,
                                                                                   OrderStatus.Confirmed);

        // Assert
        Assert.Equal(OrderStatus.Confirmed, updatedOrder.Status);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenPendingToCompletedDirectly_ShouldThrowInvalidTransition()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync();
        OrderResponseDTO order = await OrderOneProductForUser(user.Id, product);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(() =>
            _orderService.UpdateOrderStatusAsync(order.Id,
                                                 requesterId: 99,
                                                 isAdmin: true,
                                                 OrderStatus.Completed));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenCompletedOrderCancelled_ShouldThrowInvalidTransition()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync();
        OrderResponseDTO order = await OrderOneProductForUser(user.Id, product);

        // status: Pending -> Confirmed
        await _orderService.UpdateOrderStatusAsync(order.Id,
                                                   requesterId: 99,
                                                   isAdmin: true,
                                                   OrderStatus.Confirmed);

        // status: Confirmed -> Completed
        await _orderService.UpdateOrderStatusAsync(order.Id,
                                                   requesterId: 99,
                                                   isAdmin: true,
                                                   OrderStatus.Completed);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(() =>
            _orderService.UpdateOrderStatusAsync(order.Id,
                                                 requesterId: 99,
                                                 isAdmin: true,
                                                 OrderStatus.Cancelled));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenCustomerCancelsOwnPendingOrder_ShouldSucceed()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync();
        OrderResponseDTO order = await OrderOneProductForUser(user.Id, product);

        // Act
        OrderResponseDTO updated = await _orderService.UpdateOrderStatusAsync(order.Id,
                                                                              requesterId: 1,
                                                                              isAdmin: false,
                                                                              OrderStatus.Cancelled);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenCustomerCancelsOthersPendingOrder_ShouldThrowOrderNotFound()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync();
        OrderResponseDTO order = await OrderOneProductForUser(user.Id, product);

        // Act & Assert
        // should not be able to find another user's order
        await Assert.ThrowsAsync<OrderNotFoundException>(() =>
        _orderService.UpdateOrderStatusAsync(order.Id,
                                             requesterId: 2, // user 2 tries to update user 1's order
                                             isAdmin: false,
                                             OrderStatus.Cancelled));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenPendingToConfirmedByCustomer_ShouldThrowUnauthorized()
    {
        // Arrange
        AppUser user = await SeedUserAsync();
        Product product = await SeedProductAsync();
        OrderResponseDTO order = await OrderOneProductForUser(user.Id, product);

        // Act & Asser
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        _orderService.UpdateOrderStatusAsync(order.Id,
                                             requesterId: 1,
                                             isAdmin: false,
                                             OrderStatus.Confirmed));
    }


    // HELPER
    private async Task<Product> SeedProductAsync(int stock = 10,
                                                 decimal price = 25.00m,
                                                 bool isActive = true)
    {
        Category category = new() { Name = "Test Category" };
        _dataContext.Categories.Add(category);

        Product product = new()
        {
            Name = "Test",
            Description = "This is a test product.",
            Price = price,
            Stock = stock,
            Category = category,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        _dataContext.Products.Add(product);
        await _dataContext.SaveChangesAsync();

        return product;
    }

    private async Task<AppUser> SeedUserAsync(string email = "customer@test.com")
    {
        AppUser user = new()
        {
            Email = email,
            FirstName = "Test",
            LastName = "Customer",
            Age = 30,
            Role = "Customer",
            PasswordHash = "samplepassword"
        };

        _dataContext.Users.Add(user);
        await _dataContext.SaveChangesAsync();

        return user;
    }

    private async Task<OrderResponseDTO> OrderOneProductForUser(int userId, Product product)
    {
        OrderCreateDTO orderRequest = new()
        {
            OrderItemDTOs = new List<OrderItemCreateDTO>
            {
                new() {
                    ProductId = product.Id,
                    Quantity = 1
                }
            }
        };

        return await _orderService.CreateOrderAsync(userId, orderRequest);
    }
}

