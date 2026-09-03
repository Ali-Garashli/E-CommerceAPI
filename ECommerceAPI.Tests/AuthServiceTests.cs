using System;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ECommerceAPI.Tests;

public class AuthServiceTests
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DataContext _dataContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher = new PasswordHasher<AppUser>();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _sqliteConnection = new SqliteConnection("Filename=:memory:");
        _sqliteConnection.Open();
        DbContextOptions<DataContext> options =
            new DbContextOptionsBuilder<DataContext>().UseSqlite(_sqliteConnection).Options;

        _dataContext = new DataContext(options);
        // build schema on the temporary database
        _dataContext.Database.EnsureCreated();

        _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<AppUser>()))
                         .Returns("fake-jwt-token");

        _authService = new AuthService(_dataContext,
                                       _tokenServiceMock.Object,
                                       _passwordHasher);
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _sqliteConnection.Dispose();
    }


    // REGISTER
    [Fact]
    public async Task RegisterAsync_NewEmail_ShouldCreateUserWithCustomerRole()
    {
        // Arragne
        RegisterDTO registerDTO = new()
        {
            Email = "newcustomer@test.com",
            FirstName = "New",
            LastName = "Customer",
            Age = 22,
            Password = "Password123"
        };

        // Act
        await _authService.RegisterAsync(registerDTO);

        // Assert
        AppUser newUser = await _dataContext.Users.SingleAsync(u => u.Email == "newcustomer@test.com");
        Assert.Equal("Customer", newUser.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowUserEmailIsTakenException()
    {
        // Arrange
        await SeedUserAsync("existing@test.com", "SomePass123");

        RegisterDTO registerDTO = new()
        {
            Email = "existing@test.com",
            FirstName = "Dup",
            LastName = "User",
            Age = 22,
            Password = "Password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UserEmailIsTakenException>(() =>
            _authService.RegisterAsync(registerDTO));
    }

    [Fact]
    public async Task RegisterAsync_ShowldStorePasswordAsHashNotText()
    {
        // Arrange
        RegisterDTO registerDTO = new()
        {
            Email = "example@test.com",
            FirstName = "Hash",
            LastName = "Check",
            Age = 22,
            Password = "Password123"
        };

        // Act
        await _authService.RegisterAsync(registerDTO);

        // Assert
        AppUser newUser = await _dataContext.Users.SingleAsync(u => u.Email == "example@test.com");
        Assert.NotEqual("Password123", newUser.PasswordHash);
    }


    // LOGIN
    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        await SeedUserAsync("example@test.com", "Password123");

        LoginDTO loginDTO = new()
        {
            Email = "example@test.com",
            Password = "Password123"
        };

        // Act
        string token = await _authService.LoginAsync(loginDTO);

        // Assert
        Assert.Equal("fake-jwt-token", token);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        // no users have been seeded
        LoginDTO loginDTO = new()
        {
            Email = "nonexistent@test.com",
            Password = "AnyPass1"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _authService.LoginAsync(loginDTO));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        await SeedUserAsync("example@test.com", "CorrectPassword123");

        LoginDTO loginDTO = new()
        {
            Email = "example@test.com",
            Password = "WrongPassword123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _authService.LoginAsync(loginDTO));
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmailAndWrongPassword_ShouldThrowSameException()
    {
        // Arrange
        // both throw the same error to avoid leaking existing emails
        await SeedUserAsync("exists@test.com", "CorrectPassword123");

        // Act
        // non existent email:
        InvalidCredentialsException noSuchUserEx =
            await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
                _authService.LoginAsync(new LoginDTO {
                                            Email = "nonexistent@test.com",
                                            Password = "AnyPassword123"
                                        }));

        // email exists, wrong password:
        InvalidCredentialsException wrongPasswordEx =
            await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
                _authService.LoginAsync(new LoginDTO {
                                            Email = "exists@test.com",
                                            Password = "WrongPassword123"
                                        }));

        // Assert
        Assert.Equal(noSuchUserEx.Message, wrongPasswordEx.Message);
    }


    // HELPER
    private async Task<AppUser> SeedUserAsync(string email, string password)
    {
        AppUser user = new()
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Age = 25,
            Role = "Customer"
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _dataContext.Users.Add(user);
        await _dataContext.SaveChangesAsync();

        return user;
    }
}

