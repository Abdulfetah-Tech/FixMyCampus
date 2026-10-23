using System.Security.Claims;
using System.Text;
using FixMyCampus.Application.Common;
using FixMyCampus.Infrastructure;
using FixMyCampus.Infrastructure.Persistence;
using FixMyCampus.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Services
// --------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtectionKeysPath"] ?? "/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("FixMyCampus.Api");

// --------------------------------------------------
// JWT configuration
// --------------------------------------------------

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing.");
}

if (Encoding.UTF8.GetBytes(jwtSettings.Secret).Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret must be at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is missing.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is missing.");
}

// --------------------------------------------------
// Authentication
// --------------------------------------------------

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Secret)),

                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,

                ClockSkew = TimeSpan.Zero
            };

        // JWT is stored in the HttpOnly access_token cookie.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(
                        "access_token",
                        out var accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// --------------------------------------------------
// Authorization
// --------------------------------------------------

builder.Services.AddAuthorization();

// --------------------------------------------------
// CORS
// --------------------------------------------------
// Allows the Angular development server
// (http://localhost:4200) to call the API.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://localhost:8082")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --------------------------------------------------
// Build
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// Ensure database is migrated (Dev & Prod)
// --------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedDemoData"))
{
    await DbSeeder.SeedAsync(app.Services);
}

// --------------------------------------------------
// HTTP pipeline
// --------------------------------------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (status, title, detail) = exception switch
        {
            UnauthorizedAccessException ex =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            KeyNotFoundException ex =>
                (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            ArgumentException ex =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            InvalidOperationException ex =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, "Server Error",
                    "The server encountered an unexpected error.")
        };

        if (exception is not null)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("FixMyCampus.Api.ExceptionHandler");

            if (status >= StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled API exception.");
            else
                logger.LogWarning(exception, "Handled API exception as {Status}.", status);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title,
            status,
            detail,
            instance = context.Request.Path.Value
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FixMyCampus API");
    });
}
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// Serve files from wwwroot.
app.UseStaticFiles();

// Allow Angular to call the API.
app.UseCors("AngularClient");

// Read and validate JWT.
app.UseAuthentication();

// Check [Authorize] and [Authorize(Roles = "...")].
app.UseAuthorization();

// Map API controllers.
app.MapControllers();

app.Run();

public partial class Program { }

public partial class Program { }