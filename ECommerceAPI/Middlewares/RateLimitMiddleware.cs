using System.Security.Claims;
using ECommerceAPI.Attributes;
using ECommerceAPI.DTOs;
using ECommerceAPI.Services;

namespace ECommerceAPI.Middlewares;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    public RateLimitMiddleware(RequestDelegate requestDelegate)
        => _requestDelegate = requestDelegate;

    public async Task InvokeAsync(HttpContext context,
                                  RateLimitService rateLimitService)
    {
        string? policyName = GetPolicy(context);

        // skip this middleware if there is no policy
        if (policyName is null
            || context.User.IsInRole("Admin")) // policies don't apply to admins
        {
            await _requestDelegate(context);
            return;
        }

        string client = GetClientKey(context);

        RateLimitResultDTO result =
            await rateLimitService.CheckAsync(policyName, client);

        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();

        if (!result.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await _requestDelegate(context);
    }

    private static string? GetPolicy(HttpContext context)
        => context.GetEndpoint()?.Metadata
                  .GetMetadata<RateLimitPolicyAttribute>()?.PolicyName;

    private static string GetClientKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            string? userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
                return "user:" + userId;
        }

        return "ip:" + context.Connection.RemoteIpAddress?.ToString()
                       ?? "unknown";
    }
}
