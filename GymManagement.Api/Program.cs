using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using GymManagement.Api.Configuration;
using GymManagement.Api.Data;
using GymManagement.Api.Entities;
using GymManagement.Api.Services.Email;
using GymManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gym Management API",
        Version = "v1"
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter token as: Bearer {your JWT token}"
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key setting.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();

var appUrls = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
if (appUrls.Contains("https://", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        await next();
        return;
    }

    var userIdRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    Guid? userId = null;
    if (Guid.TryParse(userIdRaw, out var parsedUserId))
    {
        userId = parsedUserId;
    }

    AppUser? user = null;
    if (userId.HasValue)
    {
        var scopedDb = context.RequestServices.GetRequiredService<AppDbContext>();
        user = scopedDb.Users.FirstOrDefault(u => u.Id == userId.Value);
    }

    var isChangePasswordEndpoint =
        context.Request.Path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase);

    if (user?.MustChangePassword == true && !isChangePasswordEndpoint)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Password reset is required before accessing other features.",
            code = "PASSWORD_RESET_REQUIRED"
        });
        return;
    }

    var isWriteMethod =
        HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method);

    if (!isWriteMethod)
    {
        await next();
        return;
    }

    if (context.User.IsInRole("SuperAdmin"))
    {
        await next();
        return;
    }

    if (!userId.HasValue)
    {
        await next();
        return;
    }

    if (user?.GymTenantId is null)
    {
        await next();
        return;
    }

    var db = context.RequestServices.GetRequiredService<AppDbContext>();
    var gym = db.GymTenants.FirstOrDefault(g => g.Id == user.GymTenantId.Value);
    var isSubscriptionActivationEndpoint =
        context.Request.Path.Equals("/api/subscription/activate", StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.Equals("/api/subscription/pay", StringComparison.OrdinalIgnoreCase);

    if (!isSubscriptionActivationEndpoint && gym is not null)
    {
        var today = DateTime.UtcNow.Date;
        var validTill = gym.SubscriptionValidTill?.Date;
        var subscriptionExpired = !validTill.HasValue || validTill.Value < today;

        if (subscriptionExpired)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Subscription expired. Please purchase or renew a plan to continue new activities.",
                code = "SUBSCRIPTION_EXPIRED"
            });
            return;
        }
    }

    if (gym is not null && !gym.IsActive)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = "Gym is inactive. New activities are currently blocked." });
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "GymManagement.Api",
    status = "running",
    docs = "Use API endpoints under /api/*"
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();
