using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SignalForge.Api.Endpoints;
using SignalForge.Application;
using SignalForge.Application.Evaluation;
using SignalForge.Application.UseCases;
using SignalForge.Application.UseCases.Identity;
using Microsoft.EntityFrameworkCore;
using SignalForge.Infrastructure.Authentication;
using SignalForge.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Persistence (EF Core + PostgreSQL; in-memory DB in Testing)
builder.Services.AddSignalForgePersistence(builder.Configuration, builder.Environment);

// Identity/crypto/JWT (infrastructure)
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Rule evaluation (strategy + orchestration)
builder.Services.AddSingleton<IRuleEvaluator, SignalTypeEqualsRuleEvaluator>();
builder.Services.AddSingleton<IRuleEvaluator, SignalTypeContainsRuleEvaluator>();
builder.Services.AddSingleton<IRuleEvaluator, SignalValueGreaterThanRuleEvaluator>();
builder.Services.AddScoped<ISignalEvaluationService, SignalEvaluationService>();

// Use cases
builder.Services.AddTransient<CreateRule.Handler>();
builder.Services.AddTransient<IngestSignal.Handler>();
builder.Services.AddTransient<ListAlerts.Handler>();
builder.Services.AddTransient<ListSignals.Handler>();
builder.Services.AddTransient<GetSignal.Handler>();
builder.Services.AddTransient<GetAlert.Handler>();
builder.Services.AddTransient<AcknowledgeAlert.Handler>();
builder.Services.AddTransient<ListRules.Handler>();
builder.Services.AddTransient<GetRule.Handler>();
builder.Services.AddTransient<GetRuleAudit.Handler>();
builder.Services.AddTransient<UpdateRule.Handler>();
builder.Services.AddTransient<ActivateRule.Handler>();
builder.Services.AddTransient<DeactivateRule.Handler>();
builder.Services.AddTransient<ArchiveRule.Handler>();
builder.Services.AddTransient<UnarchiveRule.Handler>();
builder.Services.AddTransient<RegisterUser.Handler>();
builder.Services.AddTransient<LoginUser.Handler>();
builder.Services.AddTransient<GetCurrentUser.Handler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtAuthOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtAuthOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(JwtAuthOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT FAILED: {context.Exception.GetType().Name} - {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT VALIDATED");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"JWT CHALLENGE: error={context.Error}, description={context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<SignalForgeDbContext>().Database.MigrateAsync();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ArgumentException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        var message = ex.Message ?? string.Empty;
        context.Response.StatusCode = message.Contains("Invalid credentials", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!builder.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapSignalEndpoints();
app.MapRuleEndpoints();
app.MapAlertEndpoints();

app.Run();

public partial class Program { }
