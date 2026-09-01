using ECommerceAPI.Attributes;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
        => _authService = authService;

    [HttpPost("register")]
    [RateLimitPolicy("RegisterPolicy")]
    public async Task<IActionResult> Register(RegisterDTO registerDTO)
    {
        await _authService.RegisterAsync(registerDTO);
        return Ok();
    }

    [HttpPost("login")]
    [RateLimitPolicy("LoginPolicy")]
    public async Task<IActionResult> Login(LoginDTO loginDTO)
    {
        string token = await _authService.LoginAsync(loginDTO);
        return Ok(new { Token = token });
    }
}