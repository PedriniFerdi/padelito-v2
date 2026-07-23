using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Padelito.Infrastructure.Data;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public sealed class HealthController(PadelitoDbContext dbContext, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? Ok(new { status = "healthy" })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Database health check failed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
        }
    }
}
