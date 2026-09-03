namespace ECommerceAPI.Models;

public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(int productId)
        : base($"Product '{productId}' was not found.")
    { }
}

public class CategoryNotFoundException : Exception
{
    public CategoryNotFoundException(int categoryId)
        : base($"Category '{categoryId}' does not exist.")
    { }
}

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string productName,
                                      int available,
                                      int requested)
        : base($"Not enough stock for '{productName}': requested {requested}, only {available} available.")
    { }
}

public class OrderNotFoundException : Exception
{
    public OrderNotFoundException(int orderId)
        : base($"Order '{orderId}' was not found.")
    { }
}

public class InvalidOrderStatusTransitionException : Exception
{
    public InvalidOrderStatusTransitionException(OrderStatus from,
                                                 OrderStatus to)
        : base($"Cannot transition an order from '{from}' to '{to}'.")
    { }
}

public class UserNotFoundException : Exception
{
    public UserNotFoundException(int userId)
        : base($"User '{userId}' was not found.")
    { }
}

public class UserEmailIsTakenException : Exception
{
    public UserEmailIsTakenException(string email)
        : base($"A user with eamil '{email}' already exists.")
    { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Incorrect email or password.")
    { }
}
