using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Padelito.Api.Security;
using Padelito.Application.DTOs.Auth;
using Padelito.Application.DTOs.Audit;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.DTOs.Dashboard;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.DTOs.Reports;
using Padelito.Application.DTOs.Reservations;
using Padelito.Infrastructure.Data;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class DemoSessionIntegrationTests
{
    [Fact]
    public async Task Demo_writes_are_coherent_inside_one_page_load_and_never_reach_production_data()
    {
        await using var factory = new PadelitoApiFactory(demoAdmin: true);
        using var client = factory.CreateClient();
        var auth = await LoginAsync(client);
        Assert.True(auth.User.IsDemo);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/clients")).StatusCode);
        client.DefaultRequestHeaders.Add(DemoSessionMiddleware.HeaderName, Guid.NewGuid().ToString());
        var visibleUsers = await client.GetFromJsonAsync<List<UserListDto>>("/api/users");
        Assert.DoesNotContain(visibleUsers!, x => x.Username == "Ferdi");

        var customerResponse = await client.PostAsJsonAsync("/api/clients", new ClientCreateDto(
            "Temporary", "Player", "40999888", "+1 555 010 2000", "temporary.player@example.test"));
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<ClientDetailDto>())!;

        var turnResponse = await client.PostAsJsonAsync("/api/available-turns", new AvailableTurnCreateDto(
            9001, new TimeOnly(20, 0), new TimeOnly(21, 30)));
        turnResponse.EnsureSuccessStatusCode();
        var turn = (await turnResponse.Content.ReadFromJsonAsync<AvailableTurnListDto>())!;

        var reservationResponse = await client.PostAsJsonAsync("/api/reservations", new ReservationCreateDto(
            customer.Id, turn.Id, null, new DateOnly(2026, 7, 12), 1));
        reservationResponse.EnsureSuccessStatusCode();
        var reservation = (await reservationResponse.Content.ReadFromJsonAsync<ReservationDetailDto>())!;

        var paymentResponse = await client.PostAsJsonAsync("/api/payments", new PaymentCreateDto(
            reservation.Id, 1, "Temporary demo payment"));
        paymentResponse.EnsureSuccessStatusCode();

        var report = await client.GetFromJsonAsync<ReservationReportDto>(
            "/api/reports/reservations?dateFrom=2026-07-12&dateTo=2026-07-12");
        Assert.Contains(report!.Rows, row => row.ReservationId == reservation.Id && row.TotalPaid == row.FinalPrice);
        var board = await client.GetFromJsonAsync<OperationsBoardDto>("/api/reservations/operations-board");
        Assert.Contains(board!.TimelineByCourt.SelectMany(x => x.Reservations), x => x.Id == reservation.Id);
        var dashboard = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary");
        Assert.Contains(dashboard!.LatestReservations, x => x.Id == reservation.Id);
        var audits = await client.GetFromJsonAsync<List<ReservationAuditListDto>>(
            $"/api/audit/reservations?reservationId={reservation.Id}");
        Assert.Contains(audits!, x => x.ReservationId == reservation.Id && x.Action == "PaymentConfirmed");

        await using var scope = factory.Services.CreateAsyncScope();
        await using var production = scope.ServiceProvider
            .GetRequiredService<IProductionPadelitoDbContextFactory>()
            .CreateDbContext();
        Assert.False(await production.People.AnyAsync(x => x.Dni == "40999888"));
        Assert.False(await production.Reservations.AnyAsync(x => x.Id == reservation.Id));
    }

    [Fact]
    public async Task New_page_load_and_second_browser_receive_clean_isolated_snapshots()
    {
        await using var factory = new PadelitoApiFactory(demoAdmin: true);
        using var firstClient = factory.CreateClient();
        await LoginAsync(firstClient);
        firstClient.DefaultRequestHeaders.Add(DemoSessionMiddleware.HeaderName, Guid.NewGuid().ToString());
        (await firstClient.PostAsJsonAsync("/api/clients", new ClientCreateDto(
            "Only", "Here", "40999777", "+1 555 010 2001", "only.here@example.test"))).EnsureSuccessStatusCode();

        var firstItems = await firstClient.GetFromJsonAsync<List<ClientListDto>>("/api/clients");
        Assert.Contains(firstItems!, x => x.Dni == "40999777");

        firstClient.DefaultRequestHeaders.Remove(DemoSessionMiddleware.HeaderName);
        firstClient.DefaultRequestHeaders.Add(DemoSessionMiddleware.HeaderName, Guid.NewGuid().ToString());
        var reloadedItems = await firstClient.GetFromJsonAsync<List<ClientListDto>>("/api/clients");
        Assert.DoesNotContain(reloadedItems!, x => x.Dni == "40999777");

        using var secondClient = factory.CreateClient();
        await LoginAsync(secondClient);
        secondClient.DefaultRequestHeaders.Add(DemoSessionMiddleware.HeaderName, Guid.NewGuid().ToString());
        var secondItems = await secondClient.GetFromJsonAsync<List<ClientListDto>>("/api/clients");
        Assert.DoesNotContain(secondItems!, x => x.Dni == "40999777");
    }

    [Fact]
    public async Task Reception_public_account_is_also_isolated_from_production_data()
    {
        await using var factory = new PadelitoApiFactory(demoAdmin: true);
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "juanperez",
            password = "reception123"
        });
        response.EnsureSuccessStatusCode();
        Assert.True((await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.User.IsDemo);
        client.DefaultRequestHeaders.Add(DemoSessionMiddleware.HeaderName, Guid.NewGuid().ToString());

        (await client.PostAsJsonAsync("/api/clients", new ClientCreateDto(
            "Reception", "Preview", "40999666", "+1 555 010 2002", "reception.preview@example.test"))).EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        await using var production = scope.ServiceProvider
            .GetRequiredService<IProductionPadelitoDbContextFactory>()
            .CreateDbContext();
        Assert.False(await production.People.AnyAsync(x => x.Dni == "40999666"));
    }

    [Fact]
    public async Task Private_admin_writes_continue_to_use_production_database()
    {
        await using var factory = new PadelitoApiFactory(demoAdmin: true);
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "Ferdi",
            password = "private-test-password"
        });
        response.EnsureSuccessStatusCode();
        Assert.False((await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.User.IsDemo);

        (await client.PostAsJsonAsync("/api/clients", new ClientCreateDto(
            "Persistent", "Player", "40999555", "+1 555 010 2003", "persistent.player@example.test"))).EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        await using var production = scope.ServiceProvider
            .GetRequiredService<IProductionPadelitoDbContextFactory>()
            .CreateDbContext();
        Assert.True(await production.People.AnyAsync(x => x.Dni == "40999555"));
    }

    private static async Task<AuthResponseDto> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin123"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }
}
