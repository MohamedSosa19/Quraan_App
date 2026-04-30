using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.Api.Middleware;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Quraan.Application.Audio;
using Quraan.Application.Auth;
using Quraan.Application.Auth.Validators;
using Quraan.Application.Ayahs;
using Quraan.Application.Caching;
using Quraan.Application.Search;
using Quraan.Application.Surahs;
using Quraan.Application.Tafsir;
using Quraan.Application.Users;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.ExternalAudio;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Quraan.Infrastructure.Repositories;
using Quraan.Infrastructure.Seed;
using Serilog;

// CLI mode: `dotnet run -- seed` / `dotnet run -- verify-content`.
if (args.Length > 0 && (args[0] is "seed" or "verify-content"))
{
    return await CliEntry.RunAsync(args).ConfigureAwait(false);
}

var builder = WebApplication.CreateBuilder(args);

// --- Logging ---------------------------------------------------------------
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- Persistence -----------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=Quraan_Dev;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddDbContext<QuraanDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(QuraanDbContext).Assembly.GetName().Name)));

// --- Identity + JWT --------------------------------------------------------
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<QuraanDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Replace(ServiceDescriptor.Scoped<IPasswordHasher<ApplicationUser>, Argon2idPasswordHasher<ApplicationUser>>());

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "quraan-api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "quraan-app",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SigningKey"])
                        ? "DEV_SECRET_REPLACE_IN_PRODUCTION_AT_LEAST_32_BYTES_LONG_!!"
                        : builder.Configuration["Jwt:SigningKey"]!))
        };
    });
builder.Services.AddAuthorization();

// --- Caching ---------------------------------------------------------------
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
}
builder.Services.AddSingleton<ICachedReader, CachedReader>();

// --- Repositories + services ----------------------------------------------
builder.Services.AddScoped<ISurahRepository, SurahRepository>();
builder.Services.AddScoped<IAyahRepository, AyahRepository>();
builder.Services.AddScoped<ITafsirRepository, TafsirRepository>();
builder.Services.AddScoped<IBookmarkRepository, BookmarkRepository>();
builder.Services.AddScoped<ILastReadRepository, LastReadRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITranslationRepository, TranslationRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IReciterRepository, ReciterRepository>();
builder.Services.AddScoped<ISurahService, SurahService>();
builder.Services.AddScoped<IAyahService, AyahService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddSingleton<IAudioUrlBuilder, AlQuranCloudClient>();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ITafsirService, TafsirService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// FluentValidation — auto-runs on [FromBody] DTOs that have a registered validator.
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// IP rate limiter — FR-029b: 10 req/min/IP for /auth/login + /auth/register; 429
// is returned BEFORE the controller runs (which is before any password check).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers["Retry-After"] = "60";
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Title = "Too many requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Sign-in rate limit exceeded. Please retry after 60 seconds.",
        }, ct).ConfigureAwait(false);
    };
    options.AddPolicy("auth-ip", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });
});

// quran.com timing API — typed HttpClient with 5s timeout + Polly retry (R-04, FR-014).
builder.Services.AddHttpClient<IAudioTimingProvider, QuranComClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExternalAudio:QuranComBaseUrl"] ?? "https://api.quran.com");
    c.Timeout = TimeSpan.FromSeconds(5);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
    c.DefaultRequestHeaders.Add("User-Agent", "QuraanApp/1.0");
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt)));

// --- Web pipeline ----------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<QuraanDbContext>("database");

var app = builder.Build();

// --- Boot-time content-integrity gate (T055a, Principle I) -----------------
var integrityLog = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Quraan.Boot.Integrity");
try
{
    var verifier = ChecksumVerifier.ForAssemblyLocation();
    var result = await verifier.VerifyAllAsync().ConfigureAwait(false);
    if (!result.Ok)
    {
        if (app.Environment.IsDevelopment() &&
            result.Errors.All(e =>
                e.Contains("PLACEHOLDER", StringComparison.Ordinal)
                || e.Contains("missing on disk", StringComparison.Ordinal)
                || e.Contains("Manifest missing", StringComparison.Ordinal)))
        {
            integrityLog.LogWarning("Content integrity gate is in DEV-bootstrap mode (sources not yet downloaded). Proceeding.");
            foreach (var err in result.Errors) integrityLog.LogWarning("  - {Issue}", err);
        }
        else
        {
            integrityLog.LogCritical("Content integrity gate FAILED:");
            foreach (var err in result.Errors) integrityLog.LogCritical("  - {Issue}", err);
            Environment.Exit(1);
        }
    }
    else
    {
        integrityLog.LogInformation("Content integrity gate OK.");
    }
}
catch (Exception ex)
{
    integrityLog.LogCritical(ex, "Content integrity gate threw; refusing to start.");
    Environment.Exit(1);
}

// --- Pipeline order --------------------------------------------------------
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/api/v1/health/ready");

await app.RunAsync().ConfigureAwait(false);
return 0;

internal static class CliEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Quraan_Dev;Trusted_Connection=True;TrustServerCertificate=True";
        builder.Services.AddDbContext<QuraanDbContext>(o => o.UseSqlServer(connectionString));
        builder.Services.AddLogging(lb => lb.AddSimpleConsole());

        var host = builder.Build();
        switch (args[0])
        {
            case "seed":
                await SeedRunner.RunAsync(host.Services).ConfigureAwait(false);
                return 0;
            case "verify-content":
                var verifier = ChecksumVerifier.ForAssemblyLocation();
                var r = await verifier.VerifyAllAsync().ConfigureAwait(false);
                if (r.Ok) { Console.WriteLine("OK"); return 0; }
                foreach (var err in r.Errors) Console.Error.WriteLine(err);
                return 1;
            default:
                return 0;
        }
    }
}

public partial class Program;
