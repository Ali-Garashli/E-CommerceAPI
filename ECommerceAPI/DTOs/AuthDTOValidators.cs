using FluentValidation;

namespace ECommerceAPI.DTOs;

public class RegisterDTOValidator : AbstractValidator<RegisterDTO>
{
    public RegisterDTOValidator()
    {
        RuleFor(x => x.Email).NotEmpty()
                             .EmailAddress();

        RuleFor(x => x.FirstName).NotEmpty();

        RuleFor(x => x.LastName).NotEmpty();

        RuleFor(x => x.Age).GreaterThan(0)
                           .LessThan(150);

        RuleFor(x => x.Password).NotEmpty()
                                .MinimumLength(8)
                                .WithMessage("Password must be at least 8 characters.");
    }
}

public class LoginDTOValidator : AbstractValidator<LoginDTO>
{
    public LoginDTOValidator()
    {
        RuleFor(x => x.Email).NotEmpty()
                             .EmailAddress();

        RuleFor(x => x.Password).NotEmpty();
    }
}
