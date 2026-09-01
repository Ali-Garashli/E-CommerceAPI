using ECommerceAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Data;

public static class DataSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        // get DataContext and PasswordHasher
        using var scope = serviceProvider.CreateScope();
        DataContext? context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

        // make sure database is created
        await context.Database.EnsureCreatedAsync();

        // create a default admin user if it doesn't exist
        bool adminExists = await context.Users.AnyAsync(u => u.Role.Equals("Admin"));
        if (!adminExists)
        {
            AppUser defaultAdmin = new()
            {
                Email = "admin@example.com",
                Role = "Admin"
            };

            defaultAdmin.PasswordHash = passwordHasher.HashPassword(defaultAdmin, "P@ssw0rd!");

            context.Users.Add(defaultAdmin);
            await context.SaveChangesAsync();
        }
    }
}