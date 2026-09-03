using System;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Tests;

public class AppUserServiceTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DataContext _dataContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher = new PasswordHasher<AppUser>();
    private readonly AppUserService _appUserService;

    public AppUserServiceTests()
    {
        _sqliteConnection = new SqliteConnection("Filename=:memory:");
        _sqliteConnection.Open();

        DbContextOptions<DataContext> options =
            new DbContextOptionsBuilder<DataContext>().UseSqlite(_sqliteConnection).Options;

        _dataContext = new DataContext(options);
        // build schema on the temporary database
        _dataContext.Database.EnsureCreated();

        _appUserService = new AppUserService(_dataContext, _passwordHasher);
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _sqliteConnection.Dispose();
    }


    // READ
    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllSeededUsers()
    {
        // Arrange
        await SeedUserAsync("a@test.com");
        await SeedUserAsync("b@test.com");

        // Act
        List<AppUserResponseDTO> responseDTOs = await _appUserService.GetAllUsersAsync();

        // Assert
        Assert.Equal(2, responseDTOs.Count);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenOwnerRequests_ShouldReturnUser()
    {
        // Arrange
        AppUser user = await SeedUserAsync("owner@test.com");

        // Act
        AppUserResponseDTO? responseDTO = await _appUserService.GetUserByIdAsync(user.Id,
                                                                                 requesterId: user.Id,
                                                                                 isAdmin: false);

        // Assert
        Assert.NotNull(responseDTO);
        Assert.Equal(user.Email, responseDTO!.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenAdminRequestsOtherUser_ShouldReturnUser()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");

        // Act
        AppUserResponseDTO? responseDTO = await _appUserService.GetUserByIdAsync(user.Id,
                                                                                 requesterId: 999,
                                                                                 isAdmin: true);

        // Assert
        Assert.NotNull(responseDTO);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenNonAdminUserRequestsOtherUser_ShouldReturnNull()
    {
        // Arrange
        AppUser user = await SeedUserAsync("private@test.com");

        // Act
        AppUserResponseDTO? responseDTO = await _appUserService.GetUserByIdAsync(user.Id,
                                                                                 requesterId: 999,
                                                                                 isAdmin: false);

        // Assert
        Assert.Null(responseDTO);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ShouldReturnNull()
    {
        // no users are seeded
        // Act
        AppUserResponseDTO? responseDTO = await _appUserService.GetUserByIdAsync(999, // nonexistent user id
                                                                                 requesterId: 999,
                                                                                 isAdmin: true);

        // Assert
        Assert.Null(responseDTO);
    }


    // CREATE
    [Fact]
    public async Task AddUserAsync_NewEmail_ShouldCreateUser()
    {
        // Arrange
        AppUserCreateDTO requestDTO = new()
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            Age = 30,
            Role = "Customer",
            Password = "Password123"
        };

        // Act
        AppUserResponseDTO? responseDTO = await _appUserService.CreateUserAsync(requestDTO);

        // Assert
        Assert.NotEqual(0, responseDTO.Id);
        Assert.Equal("new@test.com", responseDTO.Email);
    }

    [Fact]
    public async Task AddUserAsync_DuplicateEmail_ShouldThrowUserEmailIsTakenException()
    {
        // Arrange
        await SeedUserAsync("duplicate@test.com");

        AppUserCreateDTO requestDTO = new()
        {
            Email = "duplicate@test.com",
            FirstName = "New",
            LastName = "User",
            Age = 30,
            Role = "Customer",
            Password = "Password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UserEmailIsTakenException>(() =>
            _appUserService.CreateUserAsync(requestDTO));
    }


    // UPDATE
    [Fact]
    public async Task UpdateUserAsync_WhenOwnerUpdatesUser_ShouldUpdate()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");

        AppUserUpdateDTO requestDTO = new()
        {
            Id = user.Id,
            FirstName = "UpdatedName",
            Age = 31
        };

        // Act
        AppUserResponseDTO responseDTO = await _appUserService.UpdateUserAsync(user.Id,
                                                                               requestDTO,
                                                                               requesterId: user.Id,
                                                                               isAdmin: false);

        // Assert
        Assert.Equal("UpdatedName", responseDTO.FirstName);
        Assert.Equal(31, responseDTO.Age);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNonAdminUserUpdatesOtherUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");

        AppUserUpdateDTO requestDTO = new()
        {
            Id = user.Id,
            Age = 99
        };

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _appUserService.UpdateUserAsync(user.Id,
                                            requestDTO,
                                            requesterId: 999,
                                            isAdmin: false));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenArgumentsAreEmpty_ShouldNotUpdateExistingValues()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com", "Password123");
        string originalHash = user.PasswordHash;

        AppUserUpdateDTO requestDTO = new()
        {
            Id = user.Id,
            Email = "",
            FirstName = "",
            LastName = "",
            Age = 0,
            Password = "",
        };

        // Act
        await _appUserService.UpdateUserAsync(user.Id,
                                              requestDTO,
                                              requesterId: user.Id,
                                              isAdmin: false);

        // Assert
        AppUser? updatedUser = await _dataContext.Users.FindAsync(user.Id);

        Assert.Equal("example@test.com", updatedUser!.Email);
        Assert.Equal("Test", updatedUser!.FirstName);
        Assert.Equal("User", updatedUser!.LastName);
        Assert.Equal(25, updatedUser!.Age);
        Assert.Equal(originalHash, updatedUser!.PasswordHash);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenPasswordIsProvided_ShouldChangePasswordHash()
    {
        // Arrange
        AppUser user = await SeedUserAsync("changepass@test.com", "OriginalPass1");
        string originalHash = user.PasswordHash;

        AppUserUpdateDTO requestDTO = new()
        {
            Id = user.Id,
            Age = 40,
            Password = "BrandNewPass1"
        };

        // Act
        await _appUserService.UpdateUserAsync(user.Id,
                                              requestDTO,
                                              requesterId: user.Id,
                                              isAdmin: false);

        // Assert
        AppUser? updatedUser = await _dataContext.Users.FindAsync(user.Id);

        Assert.NotEqual(originalHash, updatedUser!.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
                     _passwordHasher.VerifyHashedPassword(updatedUser,
                     updatedUser.PasswordHash,
                     "BrandNewPass1"));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenDuplicateEmailIsProvided_ShouldThrowUserEmailIsTakenException()
    {
        // Arrange
        await SeedUserAsync("taken@test.com");
        AppUser user = await SeedUserAsync("example@test.com");

        AppUserUpdateDTO requestDTO = new()
        {
            Id = user.Id,
            Age = 30,
            Email = "taken@test.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UserEmailIsTakenException>(() =>
            _appUserService.UpdateUserAsync(user.Id,
                                            requestDTO,
                                            requesterId: user.Id,
                                            isAdmin: false));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenSameEmailAsCurrentUserIsProvided_ShouldNotThrow()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");
        AppUserUpdateDTO requestDTO = new()
        {
            Id = user.Id,
            Age = 30,
            Email = "example@test.com"
        };

        // Act
        AppUserResponseDTO? responseDTO = await _appUserService.UpdateUserAsync(user.Id,
                                                                                requestDTO,
                                                                                requesterId: user.Id,
                                                                                isAdmin: false);

        // Assert
        Assert.Equal("example@test.com", responseDTO.Email);
    }


    // DELETE
    [Fact]
    public async Task DeleteUserAsync_WhenOwnerDeletesUser_ShouldRemove()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");

        // Act
        await _appUserService.DeleteUserAsync(user.Id,
                                              requesterId: user.Id,
                                              isAdmin: false);

        // Assert
        Assert.Null(await _dataContext.Users.FindAsync(user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenAdminDeletesOtherUser_ShouldRemove()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");

        // Act
        await  _appUserService.DeleteUserAsync(user.Id,
                                               requesterId: 999, // not the owner
                                               isAdmin: true);

        // Assert
        Assert.Null(await _dataContext.Users.FindAsync(user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNonAdminUserDeletesOtherUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        AppUser user = await SeedUserAsync("example@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _appUserService.DeleteUserAsync(user.Id,
                                            requesterId: 999, // not the owner
                                            isAdmin: false));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNonExistentIsRequested_ShouldThrowUserNotFoundException()
    {
        // no users have been seeded
        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _appUserService.DeleteUserAsync(999, // non existent user
                                            requesterId: 999,
                                            isAdmin: true));
    }


    // HELPER
    private async Task<AppUser> SeedUserAsync(string email,
                                              string password = "Password123",
                                              string role = "Customer")
    {
        AppUser user = new()
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Age = 25,
            Role = role
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _dataContext.Users.Add(user);
        await _dataContext.SaveChangesAsync();

        return user;
    }

}