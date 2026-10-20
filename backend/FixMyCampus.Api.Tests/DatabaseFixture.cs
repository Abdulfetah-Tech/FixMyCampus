using Microsoft.Extensions.DependencyInjection;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace FixMyCampus.Api.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("fixmycampus_tests")
        .WithUsername("postgres")
        .WithPassword("integration-test-password")
        .Build();

    public string ConnectionString => postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        using var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<AppDbContext>(options => options.UseNpgsql(ConnectionString))
            .AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .Services
            .BuildServiceProvider();

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Reporter", "Technician" })
            await roleManager.CreateAsync(new IdentityRole(role));

        await CreateUser(userManager, "reporter-one", "reporter1@test.local", "Reporter One", "Reporter", "STU-TEST-001");
        await CreateUser(userManager, "reporter-two", "reporter2@test.local", "Reporter Two", "Reporter", "STU-TEST-002");
        await CreateUser(userManager, "technician-one", "technician1@test.local", "Technician One", "Technician", "STF-TEST-001");
        await CreateUser(userManager, "technician-two", "technician2@test.local", "Technician Two", "Technician", "STF-TEST-002");
        await CreateUser(userManager, "admin", "admin@test.local", "Test Admin", "Admin", null);
    }

    public Task DisposeAsync() => postgres.DisposeAsync().AsTask();

    private static async Task CreateUser(UserManager<AppUser> manager, string id, string email, string name, string role, string? universityId)
    {
        var user = new AppUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = name,
            UniversityId = universityId,
            ApprovalStatus = ApprovalStatus.Approved,
            RequestedRole = role == "Technician" ? RegistrationRole.Technician : RegistrationRole.Reporter
        };
        var result = await manager.CreateAsync(user, "Test123!");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await manager.AddToRoleAsync(user, role);
    }
}
