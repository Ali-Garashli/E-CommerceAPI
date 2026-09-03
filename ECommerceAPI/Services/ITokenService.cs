using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

public interface ITokenService
{
    string GenerateToken(AppUser user);
}
