using FluentValidation;

namespace ECommerceAPI.DTOs;

public class OrderCreateDTOValidator : AbstractValidator<OrderCreateDTO>
{
    public OrderCreateDTOValidator()
    {
        RuleFor(x => x.OrderItemDTOs).NotEmpty()
                                     .WithMessage("There must be at least one item in the order.");

        RuleForEach(x => x.OrderItemDTOs).SetValidator(new OrderItemCreateValidator());
    }
}

public class OrderItemCreateValidator : AbstractValidator<OrderItemCreateDTO>
{
    public OrderItemCreateValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);

        RuleFor(x => x.Quantity).GreaterThan(0)
                                .WithMessage("Quantity must be at least 1.");
    }
}

