using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Padelito.Application.DTOs.Auth;
using Padelito.Application.DTOs.Audit;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.DTOs.Reports;
using Padelito.Application.DTOs.Reservations;
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
    public async Task Endpoints_require_authentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reports/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/audit/reservations")).StatusCode);
    }

    [Fact]
    public void Policies_allow_reports_for_reception_and_reserve_audit_for_admin()
    {
        var reportsPolicy = typeof(Padelito.Api.Controllers.ReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;
        var auditPolicy = typeof(Padelito.Api.Controllers.AuditController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;
        Assert.Equal("AdminOrReception", reportsPolicy);
        Assert.Equal("AdminOnly", auditPolicy);
    }
}

public sealed class PadelitoApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"padelito-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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
            scope.ServiceProvider.GetRequiredService<PadelitoDbContext>().Database.EnsureCreated();
        });
    }

}
