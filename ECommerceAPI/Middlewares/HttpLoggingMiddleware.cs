using ECommerceAPI.Attributes;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
using System.Diagnostics;
using System.Text;

namespace ECommerceAPI.Middlewares;

public class HttpLoggingMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    public HttpLoggingMiddleware(RequestDelegate requestDelegate)
        => _requestDelegate = requestDelegate;

    public async Task InvokeAsync(HttpContext httpContext,
                                  DataContext dataContext,
                                  ILogger<HttpLoggingMiddleware> logger)
    {
        // skip requests we don't want logged
        Endpoint? endpoint = httpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<NoLogAttribute>() != null
            || ShouldIgnore(httpContext.Request.Path))
        {
            await _requestDelegate(httpContext); // invoke next middleware
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        // log request
        httpContext.Request.EnableBuffering();
        string requestBody = await ReadStreamAsync(httpContext.Request.Body);

        // save metadata
        HttpLog log = new()
        {
            Method = httpContext.Request.Method,
            Path = httpContext.Request.Path,
            QueryString = httpContext.Request.QueryString.ToString(),
            RequestBody = requestBody
        };

        // intercept response stream
        Stream originalResponseBodyStream = httpContext.Response.Body;
        using MemoryStream responseBodyStream = new();
        httpContext.Response.Body = responseBodyStream;

        try
        {
            // invoke the next middleware in the pipeline
            await _requestDelegate(httpContext);
        }
        finally
        {
            stopwatch.Stop();

            // log response
            string responseBody = await ReadStreamAsync(httpContext.Response.Body);

            log.StatusCode = httpContext.Response.StatusCode;
            log.ResponseBody = responseBody;
            log.DurationMs = stopwatch.ElapsedMilliseconds;

            // save to database
            try
            {
                dataContext.HttpLogs.Add(log);
                await dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist HTTP log for {Method} {Path}", log.Method, log.Path);
            }

            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }

    private static bool ShouldIgnore(PathString path)
    {
        if (!path.HasValue)
            return false;

        string normalizedPath = path.Value.ToLowerInvariant();

        return normalizedPath.StartsWith("/swagger")      // Exclude Swagger UI and json endpoints
               || normalizedPath.EndsWith(".js")          // Exclude static JavaScript assets
               || normalizedPath.EndsWith(".css")         // Exclude static CSS stylesheets
               || normalizedPath.EndsWith(".ico")         // Exclude favicon requests
               || normalizedPath.Contains("/_framework/") // Exclude Blazor/SignalR framework files if applicable
               || normalizedPath.Contains("/api/health"); // Exclude system health checks if you have them
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        stream.Position = 0;

        using StreamReader reader = new(stream,
                                        Encoding.UTF8,
                                        leaveOpen: true);
        string text = await reader.ReadToEndAsync();

        // reset stram position
        stream.Position = 0;
        return text;
    }
}

