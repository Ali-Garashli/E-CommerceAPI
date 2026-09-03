using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Services;

public class AppUserService
{
    private readonly DataContext _dataContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AppUserService(DataContext dataContext,
                          IPasswordHasher<AppUser> passwordHasher)
    {
        _dataContext = dataContext;
        _passwordHasher = passwordHasher;
    }

    // GET ALL
    public async Task<List<AppUserResponseDTO>> GetAllUsersAsync()
    {
        List<AppUser> users = await _dataContext.Users.ToListAsync();
        return users.Select(ConvertToDTO)
                    .ToList();
    }

    // GET
    public async Task<AppUserResponseDTO?> GetUserByIdAsync(int id,
                                                            int requesterId,
                                                            bool isAdmin)
    {
        // only owner or admin can check their details
        if (requesterId != id && !isAdmin)
            return null; // return null instead of forbidden to hide the existence of a user

        AppUser? appUser = await _dataContext.Users.FindAsync(id);
        return appUser is null
               ? null
               : ConvertToDTO(appUser);
    }

    // POST
    public async Task<AppUserResponseDTO> CreateUserAsync(AppUserCreateDTO userDTO)
    {
        if (await _dataContext.Users.AnyAsync(u => u.Email == userDTO.Email))
            throw new UserEmailIsTakenException(userDTO.Email);

        AppUser newUser = new()
        {
            Email = userDTO.Email,
            FirstName = userDTO.FirstName,
            LastName = userDTO.LastName,
            Age = userDTO.Age,
            Role = userDTO.Role
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, userDTO.Password);

        _dataContext.Users.Add(newUser);
        await _dataContext.SaveChangesAsync();

        return ConvertToDTO(newUser);
    }

    // PUT
    public async Task<AppUserResponseDTO> UpdateUserAsync(int id,
                                                          AppUserUpdateDTO userDTO,
                                                          int requesterId,
                                                          bool isAdmin)
    {
        // only owner or admin can modify a profile
        if (requesterId != id && !isAdmin)
            throw new UserNotFoundException(id);

        AppUser? appUser = await _dataContext.Users.FindAsync(id)
                           ?? throw new UserNotFoundException(id);

        if (await _dataContext.Users.AnyAsync(u => u.Email == userDTO.Email
                                                   && u.Email != appUser.Email))
            throw new UserEmailIsTakenException(userDTO.Email);

        if (!string.IsNullOrEmpty(userDTO.Email))
            appUser.Email = userDTO.Email;

        if (!string.IsNullOrEmpty(userDTO.FirstName))
            appUser.FirstName = userDTO.FirstName;

        if (!string.IsNullOrEmpty(userDTO.LastName))
            appUser.LastName = userDTO.LastName;

        if (userDTO.Age > 0)
            appUser.Age = userDTO.Age;

        if (!string.IsNullOrEmpty(userDTO.Password))
            appUser.PasswordHash = _passwordHasher.HashPassword(appUser, userDTO.Password);

        await _dataContext.SaveChangesAsync();

        return ConvertToDTO(appUser);
    }

    // DELETE
    public async Task DeleteUserAsync(int id,
                                      int requesterId,
                                      bool isAdmin)
    {
        // only owner or admin can delete a profile
        if (requesterId != id && !isAdmin)
            throw new UserNotFoundException(id);

        AppUser? appUser = await _dataContext.Users.FindAsync(id)
                           ?? throw new UserNotFoundException(id);

        _dataContext.Users.Remove(appUser);
        await _dataContext.SaveChangesAsync();
    }


    // HELPER
    private static AppUserResponseDTO ConvertToDTO(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Age = user.Age,
        Role = user.Role
    };
}

