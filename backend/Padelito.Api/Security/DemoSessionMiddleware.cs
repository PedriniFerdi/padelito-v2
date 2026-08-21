using System.Security.Claims;
using Padelito.Infrastructure.Data;

namespace Padelito.Api.Security;

public sealed class DemoSessionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<DemoSessionMiddleware> logger)
{
    public const string HeaderName = "X-Demo-Session-Id";

    public async Task InvokeAsync(
        HttpContext context,
        DemoSessionRegistry registry,
        PadelitoDatabaseSessionAccessor databaseAccessor)
    {
        if (!ShouldUseDemoSession(context))
        {
            await next(context);
            return;
        }

        if (!configuration.GetValue("DemoMode:Enabled", true))
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Demo mode is unavailable.");
            return;
        }

        if (!Guid.TryParse(context.Request.Headers[HeaderName].ToString(), out var sessionId)
            || sessionId == Guid.Empty
            || !int.TryParse(context.User.FindFirstValue("UserId"), out var userId)
            || !int.TryParse(context.User.FindFirstValue("ClubId"), out var clubId))
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "A valid demo session identifier is required.");
            return;
        }

        DemoSession session;
        try
        {
            session = await registry.GetOrCreateAsync(userId, clubId, sessionId, context.RequestAborted);
        }
        catch (DemoSessionCapacityException)
        {
            await WriteProblemAsync(context, StatusCodes.Status429TooManyRequests, "The demo is at session capacity. Try again later.");
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not initialize demo session {DemoSessionId} for user {UserId}.", sessionId, userId);
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "The temporary demo session could not be initialized.");
            return;
        }

        databaseAccessor.SessionOptions = session.Options;
        var isWrite = HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsPatch(context.Request.Method)
            || HttpMethods.IsDelete(context.Request.Method);
        if (!isWrite)
        {
            await next(context);
            return;
        }

        await session.WriteLock.WaitAsync(context.RequestAborted);
        try
        {
            await next(context);
        }
        finally
        {
            session.WriteLock.Release();
        }
    }

    private static bool ShouldUseDemoSession(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/api/auth/login")
            || context.Request.Path.StartsWithSegments("/api/auth/logout")
            || context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return bool.TryParse(context.User.FindFirstValue("IsDemo"), out var isDemo) && isDemo;
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = message,
            status = statusCode
        });
    }
}
