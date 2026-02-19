using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SdnBackend.Models;
using SdnBackend.Dtos;
using SdnBackend.Services;
using SdnBackend.Repositories;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// Configure SMTP Settings for emails
builder.Services.AddSingleton<SmtpSettings>(sp =>
{
    var settings = builder.Configuration
        .GetSection("Smtp")
        .Get<SmtpSettings>();

    if (settings is null)
        throw new InvalidOperationException("Smtp settings are not configured");

    return settings;
});

builder.Services.AddTransient<IEmailService, SmtpEmailService>();

// Register shared NpgsqlDataSource (connection pool) as a singleton
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' not found");
    return NpgsqlDataSource.Create(connectionString);
});

// Register business services
builder.Services.AddScoped<SalonServicesService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<BookingService>();

// Configure JWT settings
var jwtSettings = new JwtSettings
{
    SecretKey = builder.Configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey is not configured"),
    ExpirationMinutes = builder.Configuration.GetValue<int?>("Jwt:ExpirationMinutes") ?? 30
};
builder.Services.AddSingleton(jwtSettings);

// Register JWT authentication
var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Register auth service
builder.Services.AddScoped<AuthService>();

// Register tech calendar service
builder.Services.AddScoped<TechCalendarService>();

// Register manage service
builder.Services.AddScoped<ManageService>();
builder.Services.AddScoped<AccountRepository>();

// Register background service for appointment expiration
builder.Services.AddHostedService<AppointmentExpirationService>();

// Register background service for GDPR data retention cleanup
builder.Services.AddHostedService<GdprCleanupService>();

// Force the app to listen on specific ports
builder.WebHost.UseUrls("http://localhost:5075");

// Add CORS
// TODO: tighten this up for production
//       any origin/header/method is valid only for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// use CORS
app.UseCors();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Force HTTPS
// app.UseHttpsRedirection();

// POST /services - Fetch technicians and their services for a salon
app.MapPost("/services", async (SalonServicesRequest request, SalonServicesService salonServicesService) =>
{
    var result = await salonServicesService.GetSalonTechnicianServices(request.Salon);
    if (result is null)
        return Results.NotFound(new { status = "error", message = "Salon or schedules not found" });

    return Results.Ok(result);
});

// POST /availability - Fetch available time slots for a service at a salon
app.MapPost("/availability", async (AvailabilityRequest request, AvailabilityService availabilityService) =>
{
    var (slots, errorMessage) = await availabilityService.GetAvailableSlots(request);
    if (errorMessage is not null)
        return Results.NotFound(new { status = "error", message = errorMessage });

    return Results.Ok(slots);
});

// POST /booking - Create a pending appointment and send confirmation email
app.MapPost("/booking", async (BookingRequest request, BookingService bookingService) =>
{
    var (success, errorMessage, statusCode) = await bookingService.ProcessBooking(request);
    if (!success)
    {
        return statusCode == 500
            ? Results.Problem(detail: errorMessage, statusCode: 500)
            : Results.NotFound(new { status = "error", message = errorMessage });
    }

    return Results.Ok();
});

// POST /api/bookings/confirm - Confirm an appointment via token
app.MapPost("/api/bookings/confirm", async (
    ConfirmationRequest request,
    IEmailService emailService,
    NpgsqlDataSource dataSource,
    ILogger<Program> logger) =>
{
    if (request.Token is null)
    {
        logger.LogWarning("Confirmation request received with missing token");
        throw new InvalidOperationException("Token is missing");
    }

    logger.LogInformation("Processing confirmation for token: {TokenPreview}...",
        request.Token.Substring(0, Math.Min(8, request.Token.Length)));

    try
    {
        var confirmService = new ConfirmFilingService(dataSource, Agreement.HashToken(request.Token));

        int returnValue = await confirmService.ProcessConfirmationToken(emailService);
        if (returnValue > 0)
        {
            string error = returnValue switch
            {
                1 => "No appointment found with this token",
                2 => "Token expired, new email sent",
                3 => "Appointment cancelled",
                _ => "Lookup failed",
            };
            logger.LogWarning("Confirmation failed with code {ReturnValue}: {Error}", returnValue, error);
            return Results.NotFound(new { status = "error", message = error });
        }
        else
        {
            logger.LogInformation("Appointment confirmed successfully");
            return Results.Ok();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing confirmation token. Error: {ErrorMessage}", ex.Message);
        return Results.Problem(
            detail: $"Failed to process confirmation: {ex.Message}",
            statusCode: 500);
    }
});

// POST /auth/request-password-reset - Request a password reset email
app.MapPost("/auth/request-password-reset", async (PasswordResetRequest request, AuthService authService) =>
{
    await authService.RequestPasswordReset(request.Email);
    // Always return OK to not reveal whether the email exists
    return Results.Ok();
});

// POST /auth/set-password - Set a new password using a reset token
app.MapPost("/auth/set-password", async (SetPasswordRequest request, AuthService authService) =>
{
    var (success, errorMessage) = await authService.SetPassword(request.Token, request.NewPassword);
    if (!success)
        return Results.BadRequest(new { status = "error", message = errorMessage });

    return Results.Ok();
});

// POST /auth/login - Log in and receive a JWT
app.MapPost("/auth/login", async (LoginRequest request, AuthService authService) =>
{
    var token = await authService.Login(request.Email, request.Password);
    if (token is null)
        return Results.Json(new { status = "error", message = "Invalid email or password" }, statusCode: 401);

    return Results.Ok(new { token });
});

// GET /tech/calendar?date=YYYY-MM-DD - Fetch schedules and appointments for the authenticated technician on a date
app.MapGet("/tech/calendar", async (
    string date,
    TechCalendarService techCalendarService,
    HttpContext httpContext) =>
{
    var subClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
        ?? httpContext.User.FindFirst("sub");

    if (subClaim is null || !int.TryParse(subClaim.Value, out int techId))
        return Results.Unauthorized();

    if (!DateOnly.TryParse(date, out var parsedDate))
        return Results.BadRequest(new { status = "error", message = "Invalid date format. Use YYYY-MM-DD." });

    var result = await techCalendarService.GetCalendarDay(techId, parsedDate);
    if (result is null)
        return Results.NotFound(new { status = "error", message = "Technician not found." });

    return Results.Ok(result);
}).RequireAuthorization();

// ── Manage endpoints ─────────────────────────────────────────────────────────

// Helper to extract techId from JWT claims
static int? GetTechId(HttpContext ctx)
{
    var claim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
        ?? ctx.User.FindFirst("sub");
    return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
}

// GET /tech/schedule/{id}
app.MapGet("/tech/schedule/{id:int}", async (int id, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var result = await manageService.GetScheduleDetail(id, techId);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization();

// GET /tech/appointment/{id}
app.MapGet("/tech/appointment/{id:int}", async (int id, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var result = await manageService.GetAppointmentDetail(id, techId);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization();

// PUT /tech/schedule/{id}
app.MapPut("/tech/schedule/{id:int}", async (int id, UpdateScheduleRequest request, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var (success, error) = await manageService.UpdateSchedule(id, techId, request);
    return success ? Results.Ok() : Results.BadRequest(new { status = "error", message = error });
}).RequireAuthorization();

// PUT /tech/appointment/{id}
app.MapPut("/tech/appointment/{id:int}", async (int id, UpdateAppointmentRequest request, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var (success, error, overlaps) = await manageService.UpdateAppointment(id, techId, request);
    if (!success && overlaps.Count > 0)
        return Results.Conflict(new { status = "overlap", message = error, overlaps });
    return success ? Results.Ok() : Results.BadRequest(new { status = "error", message = error });
}).RequireAuthorization();

// DELETE /tech/schedule/{id}
app.MapDelete("/tech/schedule/{id:int}", async (int id, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var (success, error) = await manageService.DeactivateSchedule(id, techId);
    return success ? Results.Ok() : Results.NotFound(new { status = "error", message = error });
}).RequireAuthorization();

// DELETE /tech/appointment/{id}
app.MapDelete("/tech/appointment/{id:int}", async (int id, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var (success, error) = await manageService.CancelAppointment(id, techId);
    return success ? Results.Ok() : Results.NotFound(new { status = "error", message = error });
}).RequireAuthorization();

// GET /tech/services — returns all available services (for new-appointment form)
app.MapGet("/tech/services", async (ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    return Results.Ok(await manageService.GetAvailableServices());
}).RequireAuthorization();

// POST /tech/schedule — create a new schedule
app.MapPost("/tech/schedule", async (CreateScheduleRequest request, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var (success, error, newId) = await manageService.CreateSchedule(techId, request);
    return success
        ? Results.Created($"/tech/schedule/{newId}", new { id = newId })
        : Results.BadRequest(new { status = "error", message = error });
}).RequireAuthorization();

// POST /tech/appointment — create a new appointment (tech-initiated, confirmed immediately)
app.MapPost("/tech/appointment", async (CreateAppointmentRequest request, ManageService manageService, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    var (success, error, newId, overlaps) = await manageService.CreateAppointment(techId, request);
    if (!success && overlaps.Count > 0)
        return Results.Conflict(new { status = "overlap", message = error, overlaps });
    return success
        ? Results.Created($"/tech/appointment/{newId}", new { id = newId })
        : Results.BadRequest(new { status = "error", message = error });
}).RequireAuthorization();

// GET /tech/settings
app.MapGet("/tech/settings", async (AccountRepository accountRepo, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    bool enabled = await accountRepo.GetEmailNotificationPreference(techId);
    return Results.Ok(new TechSettingsDto(enabled));
}).RequireAuthorization();

// PUT /tech/settings
app.MapPut("/tech/settings", async (UpdateTechSettingsRequest request, AccountRepository accountRepo, HttpContext httpContext) =>
{
    if (GetTechId(httpContext) is not int techId) return Results.Unauthorized();
    await accountRepo.SetEmailNotificationPreference(techId, request.EmailNotificationsEnabled);
    return Results.Ok();
}).RequireAuthorization();

app.Run();
