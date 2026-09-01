using FluentValidation;

namespace ECommerceAPI.DTOs;

public class ProductCreationDTOValidator : AbstractValidator<ProductCreationDTO>
{
    public ProductCreationDTOValidator()
    {
        RuleFor(x => x.Name).NotEmpty()
                            .WithMessage("Name is required.");

        RuleFor(x => x.Price).GreaterThan(0)
                             .WithMessage("Price must be positive.");

        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0)
                             .WithMessage("Stock cannot be negative.");

        RuleFor(x => x.CategoryId).GreaterThan(0)
                                  .WithMessage("There is no such category.");
    }
}

public class ProductUpdateDTOValidator : AbstractValidator<ProductUpdateDTO>
{
    public ProductUpdateDTOValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0)
                             .WithMessage("Price must be positive.");

        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0)
                             .WithMessage("Stock cannot be negative.");

        RuleFor(x => x.CategoryId).GreaterThan(0)
                                  .WithMessage("There is no such category.");
    }
}


