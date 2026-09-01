using ECommerceAPI.Attributes;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ECommerceAPI.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
[NoLog]
public class LogsController : ControllerBase
{
    private readonly DataContext _dataContext;

    public LogsController(DataContext dataContext)
        => _dataContext = dataContext;

    [HttpGet("search")]
    public async Task<ActionResult<PagedLogDTO>> SearchLogs(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? statusCode = null,
        [FromQuery] string? method = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        IQueryable<HttpLog> query = _dataContext.HttpLogs;
        // filter by HTTP method
        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(l => l.Method.Equals(method));

        // filter by status code
        if (statusCode is int stsCode)
            query = query.Where(l => l.StatusCode == stsCode);

        // filter by timestamp
        if (fromDate is DateTime frmDt)
            query = query.Where(l => l.Timestamp >= frmDt);
        if (toDate is DateTime toDt)
            query = query.Where(l => l.Timestamp <= toDt);

        // filter by search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(l => l.Path.Contains(searchTerm)
                                     || l.RequestBody.Contains(searchTerm)
                                     || l.ResponseBody.Contains(searchTerm));

        // order by newest
        query = query.OrderByDescending(l => l.Timestamp);

        int totalCount = await query.CountAsync();

        List<HttpLog> items = await query.Skip((page - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToListAsync();

        return Ok(new PagedLogDTO
        {
            LogItems = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}


