using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Services;

public class AuthService
{
    private readonly DataContext _dataContext;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthService(DataContext dataContext,
                       ITokenService tokenService,
                       IPasswordHasher<AppUser> passwordHasher)
    {
        _dataContext = dataContext;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    // REGISTER
    public async Task RegisterAsync(RegisterDTO registerDTO)
    {
        if (await _dataContext.Users.AnyAsync(u => u.Email == registerDTO.Email))
            throw new UserEmailIsTakenException(registerDTO.Email);

        AppUser newUser = new()
        {
            Email = registerDTO.Email,
            FirstName = registerDTO.FirstName,
            LastName = registerDTO.LastName,
            Age = registerDTO.Age,
            Role = "Customer"
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, registerDTO.Password);

        _dataContext.Users.Add(newUser);
        await _dataContext.SaveChangesAsync();
    }

    // LOGIN
    public async Task<string> LoginAsync(LoginDTO loginDTO)
    {
        AppUser? appUser = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email.Equals(loginDTO.Email))
                           ?? throw new InvalidCredentialsException();

        var result = _passwordHasher.VerifyHashedPassword(appUser,
                                                          appUser.PasswordHash,
                                                          loginDTO.Password);
        if (result.Equals(PasswordVerificationResult.Failed))
            throw new InvalidCredentialsException();

        return _tokenService.GenerateToken(appUser);
    }
}

