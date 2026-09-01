using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class RateLimitController : ControllerBase
{
    private readonly DataContext _dataContext;

    public RateLimitController(DataContext dataContext)
        => _dataContext = dataContext;

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        RateLimitPolicy? policy = await _dataContext.RateLimitPolicies
            .FirstOrDefaultAsync(p => p.Name.Equals(name));

        if (policy is null)
            return NotFound();

        return Ok(policy);
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> Update(string name,
                                            RateLimitUpdateDTO rateLimitDTO)
    {
        RateLimitPolicy? policy = await _dataContext.RateLimitPolicies
            .FirstOrDefaultAsync(p => p.Name.Equals(name));

        if (policy is null)
            return NotFound();

        policy.PermitLimit = rateLimitDTO.PermitLimit;
        policy.WindowSeconds = rateLimitDTO.WindowSeconds;
        policy.Enabled = rateLimitDTO.Enabled;

        await _dataContext.SaveChangesAsync();

        return Ok(policy);
    }
}

