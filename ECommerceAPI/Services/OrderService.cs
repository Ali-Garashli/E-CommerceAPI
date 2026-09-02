using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Services;

public class OrderService
{
    private readonly DataContext _dataContext;

    public OrderService(DataContext dataContext)
        => _dataContext = dataContext;

    // GET ALL
    public async Task<List<OrderResponseDTO>> GetAllOrdersAsync()
    {
        IQueryable<Order> query = _dataContext.Orders.Include(o => o.OrderItems)
                                       .ThenInclude(i => i.Product)
                                       .AsQueryable();

        // order by newest
        List<Order> orders = await query.OrderByDescending(o => o.CreatedAt)
                                        .ToListAsync();

        return orders.Select(OrderResponseDTO.ConvertToDTO)
                     .ToList();
    }

    // GET
    public async Task<OrderResponseDTO?> GetOrderByIdAsync(int orderId,
                                                           int requesterId,
                                                           bool isAdmin)
    {
        Order? order = await _dataContext.Orders.Include(o => o.OrderItems)
                                                .ThenInclude(i => i.Product)
                                                .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return null;

        if (!isAdmin && order.UserId != requesterId)
            return null;

        return OrderResponseDTO.ConvertToDTO(order);
    }


    // POST
    public async Task<OrderResponseDTO> CreateOrderAsync(int userId,
                                                         OrderCreateDTO orderDTO)
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            await using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                Order order = new()
                {
                    UserId = userId,
                    Status = OrderStatus.Pending
                };

                decimal total = 0m;

                var groupedItems = orderDTO.OrderItemDTOs.GroupBy(i => i.ProductId)
                                                         .Select(g => new {
                                                             ProductId = g.Key,
                                                             Quantity = g.Sum(i => i.Quantity)
                                                         });

                foreach (var line in groupedItems)
                {
                    Product? product = await _dataContext.Products
                        .FirstOrDefaultAsync(p => p.Id == line.ProductId && p.IsActive)
                        ?? throw new ProductNotFoundException(line.ProductId);

                    if (product.Stock < line.Quantity)
                        throw new InsufficientStockException(product.Name,
                                                             product.Stock,
                                                             line.Quantity);

                    product.Stock -= line.Quantity;

                    OrderItem? orderItem = new()
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = line.Quantity,
                        ProductPrice = product.Price // price when purchase happened
                    };

                    order.OrderItems.Add(orderItem);
                    total += orderItem.TotalPrice;
                }

                order.TotalAmount = total;

                order.OrderStatusHistory.Add(new OrderStatusHistory
                                             {
                                                 Order = order,
                                                 StatusFrom = null,
                                                 StatusTo = OrderStatus.Pending
                                             });

                _dataContext.Orders.Add(order);

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return OrderResponseDTO.ConvertToDTO(order);
            }
            // if another request changed stock in the meantime
            catch (DbUpdateConcurrencyException) when (attempt < 5)
            {
                // roll back and retry
                await transaction.RollbackAsync();
                _dataContext.ChangeTracker.Clear();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        throw new InvalidOperationException("Could not complete the order due to repeated" +
                                            "concurrent stock updates. Please try again.");
    }

    public async Task<OrderResponseDTO> UpdateOrderStatusAsync(int orderId,
                                                               int requesterId,
                                                               bool isAdmin,
                                                               OrderStatus newStatus)
    {
        Order? order = await _dataContext.Orders.Include(o => o.OrderItems)
                                                .ThenInclude(oi => oi.Product)
                                                .Include(o => o.OrderStatusHistory)
                                                .FirstOrDefaultAsync(o => o.Id == orderId)
                                                ?? throw new OrderNotFoundException(orderId);

        if (!isAdmin && order.UserId != requesterId)
            throw new OrderNotFoundException(orderId);

        // only admin can set status other than cancelled
        if (!isAdmin && newStatus != OrderStatus.Cancelled)
            throw new UnauthorizedAccessException("Customers can only cancel their orders.");

        // Only these status transitions are allowed:
        // Pending → Confirmed → Completed
        // Pending → Cancelled
        Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.Completed },
            [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        if (!AllowedTransitions.TryGetValue(order.Status, out var allowed)
            || !allowed.Contains(newStatus))
            throw new InvalidOrderStatusTransitionException(order.Status, newStatus);

        OrderStatus previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        order.OrderStatusHistory.Add(new OrderStatusHistory
                                     {
                                         OrderId = order.Id,
                                         StatusFrom = previousStatus,
                                         StatusTo = newStatus
                                     });

        await _dataContext.SaveChangesAsync();

        return OrderResponseDTO.ConvertToDTO(order);
    }
}

