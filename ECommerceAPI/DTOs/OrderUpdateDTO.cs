using ECommerceAPI.Models;
using FluentValidation;

namespace ECommerceAPI.DTOs;

public class OrderUpdateDTO
{
    public OrderStatus NewStatus { get; set; }
}

public class OrderUpdateDTOValidator : AbstractValidator<OrderUpdateDTO>
{
    public OrderUpdateDTOValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
