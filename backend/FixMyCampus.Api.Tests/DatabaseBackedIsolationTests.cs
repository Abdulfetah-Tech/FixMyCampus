using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FixMyCampus.Api.Controllers;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FixMyCampus.Api.Tests;

public sealed class DatabaseBackedIsolationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture database;
    private AuthenticatedApiFactory factory = null!;
    private int reporterOneTicketId;

    public DatabaseBackedIsolationTests(DatabaseFixture database) => this.database = database;

    public async Task InitializeAsync()
    {
        factory = new AuthenticatedApiFactory(database.ConnectionString);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(database.ConnectionString).Options;
        await using var db = new AppDbContext(options);
        var building = new Building { Name = $"Test Building {Guid.NewGuid():N}", Code = $"T{Guid.NewGuid():N}"[..10] };
        db.Buildings.Add(building);
        await db.SaveChangesAsync();
        var ticket = new Ticket { BuildingId = building.Id, ReporterId = "reporter-one", Room = "101", Description = "Database-backed isolation ticket", Category = TicketCategory.Other };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        reporterOneTicketId = ticket.Id;
    }

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Reporter_two_cannot_read_reporter_one_ticket()
    {
        var response = await factory.CreateClientFor("reporter-two", "Reporter").GetAsync($"/api/tickets/{reporterOneTicketId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Technician_two_cannot_change_reporter_one_ticket()
    {
        using var client = factory.CreateClientFor("technician-two", "Technician");
        using var body = new StringContent("{\"newStatus\":\"Assigned\"}");
        body.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.PostAsync($"/api/tickets/{reporterOneTicketId}/status", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_read_reporter_one_ticket()
    {
        var response = await factory.CreateClientFor("admin", "Admin").GetAsync($"/api/tickets/{reporterOneTicketId}");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class AuthenticatedApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing").UseSetting("ConnectionStrings:DefaultConnection", connectionString).UseSetting("Jwt:Secret", "integration-test-secret-at-least-32-bytes-long");
        builder.ConfigureServices(services => services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "TestAuth";
            options.DefaultChallengeScheme = "TestAuth";
        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", _ => { }));
    }

    public HttpClient CreateClientFor(string id, string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", id);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }
}

public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = Request.Headers["X-Test-User"].ToString();
        var role = Request.Headers["X-Test-Role"].ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(role)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role) }, "TestAuth");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "TestAuth")));
    }
}
