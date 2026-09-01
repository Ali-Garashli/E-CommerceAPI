using ECommerceAPI.Attributes;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppUserController : ControllerBase
{
    private readonly AppUserService _appUserService;

    public AppUserController(AppUserService appUserService)
        => _appUserService = appUserService;

    private int CurrentUserId
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id)
           ? id
           : 0;
    private bool IsAdmin => User.IsInRole("Admin");

    // GET ALL
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUserResponseDTO>>> GetUsers()
        => Ok(await _appUserService.GetAllUsersAsync());

    // GET
    [HttpGet("{id}")]
    [RateLimitPolicy("UserReadPolicy")]
    public async Task<ActionResult<AppUserResponseDTO>> GetUser(int id)
    {
        AppUserResponseDTO? user = await _appUserService.GetUserByIdAsync(id, CurrentUserId, IsAdmin);

        return user is null
               ? NotFound()
               : Ok(user);
    }

    // POST
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddUser(AppUserCreateDTO userDTO)
    {
        AppUserResponseDTO newUser = await _appUserService.CreateUserAsync(userDTO);

        return CreatedAtAction(nameof(GetUser),
                               new { id = newUser.Id },
                               newUser);
    }

    // PUT
    [HttpPut("{id}")]
    [RateLimitPolicy("UserWritePolicy")]
    public async Task<IActionResult> UpdateUser(int id, AppUserUpdateDTO userDTO)
    {
        if (id != userDTO.Id)
            return BadRequest();

        AppUserResponseDTO user = await _appUserService.UpdateUserAsync(id,
                                                                        userDTO,
                                                                        CurrentUserId,
                                                                        IsAdmin);

        return Ok(user);
    }

    // DELETE
    [HttpDelete("{id}")]
    [RateLimitPolicy("UserWritePolicy")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _appUserService.DeleteUserAsync(id, CurrentUserId, IsAdmin);
        return NoContent();
    }
}