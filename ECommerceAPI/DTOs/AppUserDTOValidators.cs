using FluentValidation;

namespace ECommerceAPI.DTOs;

public class AppUserCreateDTOValidator : AbstractValidator<AppUserCreateDTO>
{
    public AppUserCreateDTOValidator()
    {
        RuleFor(x => x.Email).NotEmpty()
                             .EmailAddress();

        RuleFor(x => x.FirstName).NotEmpty();

        RuleFor(x => x.LastName).NotEmpty();

        RuleFor(x => x.Age).GreaterThan(0);

        RuleFor(x => x.Password).NotEmpty()
                                .MinimumLength(8)
                                .WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.Role).NotEmpty();
    }
}

public class AppUserUpdateDTOValidator : AbstractValidator<AppUserUpdateDTO>
{
    public AppUserUpdateDTOValidator()
    {
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Age).GreaterThan(0);

        RuleFor(x => x.Password).MinimumLength(8)
                                .WithMessage("Password must be at least 8 characters long.")
                                .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

