using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Padelito.Api.Security;
using Padelito.Api.Startup;
using Padelito.Application.Interfaces.Security;
using Padelito.Application.Interfaces.Services;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Extensions;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);
ValidateProductionConfiguration(builder);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ =>
{
    var timeZoneId = builder.Configuration["Club:TimeZone"] ?? "America/New_York";
    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[AuthCookie.Name];
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrReception", policy => policy.RequireRole("Admin", "Reception"));
    options.AddPolicy("AuthenticatedStaff", policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

await ProductionBootstrapper.InitializeAsync(app.Services, app.Configuration, app.Logger);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; img-src 'self' data:; font-src 'self'; style-src 'self'; script-src 'self'; connect-src 'self'";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            return Task.CompletedTask;
        });
        await next();
    });
}
else
{
    app.UseCors(FrontendCorsPolicy);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/health"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();

static void ValidateProductionConfiguration(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsProduction())
    {
        return;
    }

    SqlConnectionStringBuilder sqlConnection;
    try
    {
        sqlConnection = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("PadelitoDb"));
    }
    catch (Exception exception)
    {
        throw new InvalidOperationException("Production requires a valid ConnectionStrings__PadelitoDb value.", exception);
    }

    if (string.IsNullOrWhiteSpace(sqlConnection.DataSource)
        || sqlConnection.DataSource.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || sqlConnection.DataSource.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
        || sqlConnection.IntegratedSecurity
        || string.IsNullOrWhiteSpace(sqlConnection.UserID)
        || string.IsNullOrWhiteSpace(sqlConnection.Password))
    {
        throw new InvalidOperationException("Production requires ConnectionStrings__PadelitoDb with MonsterASP.NET SQL credentials.");
    }

    var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
    if (jwtKey.Length < 32 || jwtKey.Contains("Development-Only", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Production requires a random Jwt__Key of at least 32 characters.");
    }

    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"])
        || string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]))
    {
        throw new InvalidOperationException("Production requires Jwt__Issuer and Jwt__Audience.");
    }

    var allowedHosts = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
    {
        throw new InvalidOperationException("Production requires AllowedHosts to contain the deployed hostname.");
    }
}

public partial class Program;
