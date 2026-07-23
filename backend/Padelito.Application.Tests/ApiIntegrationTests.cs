using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Padelito.Application.DTOs.Auth;
using Padelito.Application.DTOs.Audit;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.DTOs.Dashboard;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.DTOs.Reports;
using Padelito.Application.DTOs.Reservations;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class ApiIntegrationTests : IClassFixture<PadelitoApiFactory>
{
    private readonly PadelitoApiFactory factory;
    public ApiIntegrationTests(PadelitoApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Admin_can_complete_reservation_payment_report_csv_and_audit_flow()
    {
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = "admin", Password = "admin123" });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var createdResponse = await client.PostAsJsonAsync("/api/reservations", new ReservationCreateDto(9001, 9001, null, new(2026, 7, 14), 2));
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<ReservationDetailDto>();
        var paymentResponse = await client.PostAsJsonAsync("/api/payments", new PaymentCreateDto(created!.Id, 1, created.FinalPrice, new(2026,7,12,15,0,0,DateTimeKind.Utc), "Integración"));
        paymentResponse.EnsureSuccessStatusCode();

        var report = await client.GetFromJsonAsync<ReservationReportDto>("/api/reports/reservations?dateFrom=2026-07-14&dateTo=2026-07-14");
        var row = Assert.Single(report!.Rows);
        Assert.Equal(created.Id, row.ReservationId);
        Assert.Equal(row.FinalPrice, report.Summary.TotalPaid);
        Assert.Equal(0, report.Summary.PendingBalance);

        var csv = await client.GetAsync("/api/reports/reservations/export?dateFrom=2026-07-14&dateTo=2026-07-14");
        csv.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Lucía", await csv.Content.ReadAsStringAsync());

        var audits = await client.GetFromJsonAsync<List<ReservationAuditListDto>>($"/api/audit/reservations?reservationId={created.Id}");
        Assert.Contains(audits!, x => x.Action == "Creacion" && x.Username == "admin");
    }

    [Fact]
    public async Task Dashboard_revenue_intelligence_returns_club_scoped_metrics()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var createdResponse = await client.PostAsJsonAsync("/api/reservations", new ReservationCreateDto(9001, 9001, null, new(2026, 7, 15), 2));
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<ReservationDetailDto>();
        var paymentResponse = await client.PostAsJsonAsync("/api/payments", new PaymentCreateDto(created!.Id, 1, 1000m, new(2026, 7, 15, 15, 0, 0, DateTimeKind.Utc), "Dashboard"));
        paymentResponse.EnsureSuccessStatusCode();

        var dashboard = await client.GetFromJsonAsync<DashboardRevenueIntelligenceDto>("/api/dashboard/revenue-intelligence?dateFrom=2026-07-15&dateTo=2026-07-15");

        Assert.NotNull(dashboard);
        Assert.Equal(new DateOnly(2026, 7, 15), dashboard!.DateFrom);
        Assert.Equal(1000m, dashboard.Summary.TotalRevenue);
        Assert.Equal(100m, dashboard.Summary.AverageOccupancyRate);
        var court = Assert.Single(dashboard.Courts);
        Assert.Equal("Central Test", court.CourtName);
        Assert.Equal(1, court.ReservedSlots);
    }

    [Fact]
    public async Task Endpoints_require_authentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reports/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/audit/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/dashboard/revenue-intelligence")).StatusCode);
    }

    [Fact]
    public void Policies_allow_reports_for_reception_and_reserve_audit_for_admin()
    {
        var reportsPolicy = typeof(Padelito.Api.Controllers.ReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;
        var auditPolicy = typeof(Padelito.Api.Controllers.AuditController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;
        Assert.Equal("AdminOrReception", reportsPolicy);
        Assert.Equal("AdminOnly", auditPolicy);
    }

    [Fact]
    public async Task Client_and_employee_creation_reject_missing_or_invalid_person_fields()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var emptyClient = await client.PostAsJsonAsync("/api/clients", new
        {
            firstName = " ", lastName = "", dni = (string?)null, phone = (string?)null, email = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyClient.StatusCode);
        var emptyProblem = await emptyClient.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("FirstName", emptyProblem!.Errors.Keys);
        Assert.Contains("Dni", emptyProblem.Errors.Keys);
        Assert.Contains("Phone", emptyProblem.Errors.Keys);
        Assert.Contains("Email", emptyProblem.Errors.Keys);

        var invalidEmployee = await client.PostAsJsonAsync("/api/employees", new EmployeeCreateDto(
            "Ana", "Paz", "12A", "abc", "correo-invalido"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidEmployee.StatusCode);
    }

    [Fact]
    public async Task Client_creation_normalizes_valid_person_data_and_rejects_duplicate_dni()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var request = new ClientCreateDto("  Ana  ", "  Paz ", "40.123.456", "+54 11 4444-5555", " ANA@EXAMPLE.COM ");

        var response = await client.PostAsJsonAsync("/api/clients", request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ClientDetailDto>();
        Assert.Equal("Ana", created!.FirstName);
        Assert.Equal("40123456", created.Dni);
        Assert.Equal("ana@example.com", created.Email);

        var duplicate = await client.PostAsJsonAsync("/api/employees", new EmployeeCreateDto(
            "Otra", "Persona", "40123456", "1144445555", "otra@example.com"));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task Other_creation_endpoints_reject_empty_or_default_payloads()
    {
        using var client = await CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/users", new UserCreateDto(" ", "short", 0, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/courts", new CourtCreateDto(" ", 0, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/available-turns", new AvailableTurnCreateDto(0, default, default))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/promotions", new PromotionCreateDto(" ", null, 0, default, default))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/reservations", new ReservationCreateDto(0, 0, null, default, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/payments", new PaymentCreateDto(0, 0, 0, default, null))).StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = "admin", Password = "admin123" });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}

public sealed class PadelitoApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"padelito-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PadelitoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PadelitoDbContext>>();
            services.RemoveAll<IDatabaseProvider>();
            services.RemoveAll<PadelitoDbContext>();
            services.AddDbContext<PadelitoDbContext>(options => options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero)));
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
            dbContext.Database.EnsureCreated();
            SeedApiTestData(dbContext);
        });
    }

    private static void SeedApiTestData(PadelitoDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var club = new Club { Id = 1, Name = "Padelito Test", IsActive = true, CreatedAt = now };
        var adminPerson = new Person
        {
            Id = 1,
            FirstName = "Admin",
            LastName = "Test",
            Dni = "30111222",
            Phone = "1140001001",
            Email = "admin@test.local",
            IsActive = true,
            CreatedAt = now
        };
        var clientPerson = new Person
        {
            Id = 9001,
            FirstName = "Lucía",
            LastName = "Fernández",
            Dni = "35421678",
            Phone = "1158214076",
            Email = "lucia@example.com",
            IsActive = true,
            CreatedAt = now
        };
        var employee = new Employee { Id = 1, PersonId = 1, ClubId = 1 };
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = string.Empty,
            EmployeeId = 1,
            RoleId = 1,
            IsActive = true,
            CreatedAt = now
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "admin123");

        dbContext.AddRange(
            club,
            adminPerson,
            clientPerson,
            new Client { Id = 9001, PersonId = 9001 },
            employee,
            user,
            new Court { Id = 9001, ClubId = 1, CourtTypeId = 2, Name = "Central Test", HourPrice = 18000m, IsActive = true },
            new AvailableTurn { Id = 9001, CourtId = 9001, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 30), IsActive = true });
        dbContext.SaveChanges();
    }

}
