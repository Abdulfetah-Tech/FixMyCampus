using FixMyCampus.Domain.Common;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Domain.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixMyCampus.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in new[] { Roles.Admin, Roles.Reporter, Roles.Technician })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await EnsureUserAsync(userManager, "admin@hackathon.local", "Admin123!",
            "System Admin", null, Roles.Admin, RegistrationRole.Reporter);
        var reporter = await EnsureUserAsync(userManager, "user@hackathon.local", "User123!",
            "Demo Reporter", "STU-0001", Roles.Reporter, RegistrationRole.Reporter);
        var techUser = await EnsureUserAsync(userManager, "tech@hackathon.local", "Tech123!",
            "Demo Technician", "STF-0001", Roles.Technician, RegistrationRole.Technician);
        await EnsurePendingUserAsync(userManager, "pending@hackathon.local", "Pending123!",
            "Pending Student", "STU-0002");

        if (!await db.Buildings.AnyAsync())
        {
            db.Buildings.AddRange(
                new Building { Name = "Technology Building", Code = "TECH" },
                new Building { Name = "Main Library", Code = "LIB" },
                new Building { Name = "Lecture Hall Block A", Code = "LHA" },
                new Building { Name = "Computer Lab Center", Code = "LAB" },
                new Building { Name = "Student Dormitory", Code = "DORM" },
                new Building { Name = "Cafeteria", Code = "CAF" });
        }

        if (!await db.Technicians.AnyAsync())
        {
            db.Technicians.AddRange(
                new Technician { FullName = "Dawit Bekele" },
                new Technician { FullName = "Selam Tesfaye" },
                new Technician { FullName = "Abel Mekonnen" },
                new Technician { FullName = techUser.FullName, UserId = techUser.Id });
        }

        await db.SaveChangesAsync();

        if (!await db.Tickets.AnyAsync())
        {
            var buildings = await db.Buildings.OrderBy(b => b.Id).ToListAsync();
            var technician = await db.Technicians.OrderBy(t => t.Id).FirstAsync();

            db.Tickets.AddRange(
                BuildTicket(reporter, admin, buildings[0], technician, TicketCategory.Computer, "Lab 204",
                    "Five computers in the lab do not boot after the power cut.", TicketUrgency.High, TicketStatus.New, 1),
                BuildTicket(reporter, admin, buildings[1], technician, TicketCategory.Network, "Reading Hall",
                    "Wi-Fi access point is down, no signal on the second floor.", TicketUrgency.Medium, TicketStatus.New, 2),
                BuildTicket(reporter, admin, buildings[2], technician, TicketCategory.Projector, "Room 12",
                    "Projector shows a green screen and flickers.", TicketUrgency.Medium, TicketStatus.Assigned, 3),
                BuildTicket(reporter, admin, buildings[4], technician, TicketCategory.Plumbing, "Block C, 2nd floor",
                    "Water leaking from the ceiling near the bathroom.", TicketUrgency.High, TicketStatus.InProgress, 4),
                BuildTicket(reporter, admin, buildings[3], technician, TicketCategory.Electrical, "Lab 101",
                    "Two power outlets are dead at the back desks.", TicketUrgency.Low, TicketStatus.Resolved, 6),
                BuildTicket(reporter, admin, buildings[5], technician, TicketCategory.Other, "Main hall",
                    "Broken chair near the entrance.", TicketUrgency.Low, TicketStatus.Resolved, 8, confirmed: true));

            await db.SaveChangesAsync();
        }
    }

    private static async Task<AppUser> EnsureUserAsync(
        UserManager<AppUser> userManager, string email, string password, string fullName,
        string? universityId, string role, RegistrationRole requestedRole)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            UniversityId = universityId,
            RequestedRole = requestedRole,
            ApprovalStatus = ApprovalStatus.Approved,
            ReviewedAt = DateTime.UtcNow
        };

        await ThrowIfFailed(await userManager.CreateAsync(user, password), email);
        await ThrowIfFailed(await userManager.AddToRoleAsync(user, role), email);
        return user;
    }

    private static async Task EnsurePendingUserAsync(
        UserManager<AppUser> userManager, string email, string password, string fullName, string universityId)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            UniversityId = universityId,
            RequestedRole = RegistrationRole.Reporter,
            ApprovalStatus = ApprovalStatus.Pending
        };

        await ThrowIfFailed(await userManager.CreateAsync(user, password), email);
    }

    private static Task ThrowIfFailed(IdentityResult result, string email)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seeding '{email}' failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return Task.CompletedTask;
    }

    private static Ticket BuildTicket(
        AppUser reporter, AppUser admin, Building building, Technician technician,
        TicketCategory category, string room, string description,
        TicketUrgency urgency, TicketStatus target, int daysAgo, bool confirmed = false)
    {
        var createdAt = DateTime.UtcNow.AddDays(-daysAgo);

        var ticket = new Ticket
        {
            Category = category,
            Building = building,
            Room = room,
            Description = description,
            Urgency = urgency,
            Reporter = reporter,
            Status = TicketStatus.New,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        ticket.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = null,
            ToStatus = TicketStatus.New,
            ChangedByUser = reporter,
            ChangedAt = createdAt,
            Note = "Ticket created"
        });

        var current = TicketStatus.New;
        var changedAt = createdAt;

        while (current != target && TicketWorkflow.TryGetNext(current, out var next))
        {
            changedAt = changedAt.AddHours(3);

            ticket.StatusHistory.Add(new TicketStatusHistory
            {
                FromStatus = current,
                ToStatus = next,
                ChangedByUser = admin,
                ChangedAt = changedAt
            });

            if (next == TicketStatus.Assigned)
            {
                ticket.AssignedTechnician = technician;
                ticket.AssignedAt = changedAt;
            }

            if (next == TicketStatus.Resolved)
                ticket.ResolvedAt = changedAt;

            current = next;
        }

        ticket.Status = current;
        ticket.UpdatedAt = changedAt;

        if (confirmed && current == TicketStatus.Resolved)
            ticket.ConfirmedFixedAt = changedAt.AddHours(2);

        return ticket;
    }
}