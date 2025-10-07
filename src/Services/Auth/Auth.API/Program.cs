using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// --- Infra: MassTransit (RabbitMQ) ---
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, bus) =>
    {
        bus.Host(cfg["Rabbit:Host"] ?? "localhost", "/", h =>
        {
            h.Username(cfg["Rabbit:User"] ?? "guest");
            h.Password(cfg["Rabbit:Pass"] ?? "guest");
        });

        // service prefix, if you want named endpoints later
        bus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("svc-auth", false));
    });
});

// --- (Optional) if you want to protect some endpoints in Auth.API ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false; // dev only
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKey"] ?? "dev-signing-key-please-change")),
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });
builder.Services.AddAuthorization();

// --- Dev user store (in-memory) ---
builder.Services.AddSingleton<UserStore>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Auth API is UP!");
logger.LogInformation($"Current environment: {app.Environment.EnvironmentName}");
logger.LogInformation($"Jwt:SigningKey: {cfg["Jwt:SigningKey"]}");

app.MapGet("/health", () => Results.Ok("ok"));

// POST /auth/register  -> creates user, publishes UserRegistered, returns tokens
app.MapPost("/auth/register", async (RegisterRequest req, UserStore users, IBus bus) =>
{
    // Basic input sanity (keep it light for now)
    if (string.IsNullOrWhiteSpace(req.Email) && string.IsNullOrWhiteSpace(req.Phone))
        return Results.BadRequest("email or phone is required");

    // Create or reuse user id
    var userId = string.IsNullOrWhiteSpace(req.UserId) ? Guid.NewGuid().ToString("N") : req.UserId;

    // Very basic password handling (dev only!)
    if (!users.TryAdd(userId, new User(userId, req.Email, req.Phone, req.Password)))
        return Results.Conflict("user already exists");

    // Publish domain event for Customer to initialize profile (async decoupling)
    await bus.Publish(new UserRegistered(userId, req.Phone ?? string.Empty));

    // Issue JWTs (HS256 dev key)
    Console.WriteLine(cfg["Jwt:SigningKey"]);
    var accessToken = JwtIssuer.Issue(userId, cfg["Jwt:SigningKey"] ?? "this_is_a_very_long_secret_key_32bytess", expiresMinutes: 60);
    var refreshToken = Guid.NewGuid().ToString("N"); // stub

    return Results.Ok(new { userId, accessToken, refreshToken });
});

// POST /auth/login  -> verifies creds and returns tokens
app.MapPost("/auth/login", (LoginRequest req, UserStore users) =>
{
    if (!users.TryGet(req.UserId, out var user))
        return Results.Unauthorized();

    if (!string.Equals(user.Password, req.Password, StringComparison.Ordinal))
        return Results.Unauthorized();

    var token = JwtIssuer.Issue(user.UserId, cfg["Jwt:SigningKey"] ?? "this_is_a_very_long_secret_key_32bytess", expiresMinutes: 60);
    return Results.Ok(new { accessToken = token, refreshToken = "fake-refresh" });
});

// (optional) GET /auth/me -> show claims from the token
app.MapGet("/auth/me", (ClaimsPrincipal me) =>
{
    var sub = me.FindFirstValue("sub");
    var email = me.FindFirstValue("email");
    return Results.Ok(new { sub, email });
}).RequireAuthorization();

app.Run();

// ====== Support types ======
record RegisterRequest(string? UserId, string? Email, string? Phone, string? Password);
record LoginRequest(string UserId, string Password);

class User(string userId, string? email, string? phone, string? password)
{
    public string UserId { get; } = userId;
    public string? Email { get; } = email;
    public string? Phone { get; } = phone;
    public string? Password { get; } = password;
}

class UserStore
{
    private readonly ConcurrentDictionary<string, User> _users = new();
    public bool TryAdd(string userId, User user) => _users.TryAdd(userId, user);
    public bool TryGet(string userId, out User user) => _users.TryGetValue(userId, out user!);
}

static class JwtIssuer
{
    public static string Issue(string userId, string signingKey, int expiresMinutes)
    {
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var jwt = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: new[]
            {
                new Claim("sub", userId),
                new Claim("typ", "access"),
                new Claim("role", "user")
            },
            notBefore: now,
            expires: now.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
