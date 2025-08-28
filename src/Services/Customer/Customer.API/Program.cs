using Common.Contracts;
using Common.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

var b = WebApplication.CreateBuilder(args);
var cfg = b.Configuration;

b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearer(o => o.RequireHttpsMetadata = false);
b.Services.AddAuthorization();
b.Services.AddHealthChecks();
b.Services.AddAppBus(cfg, "svc-customer");

var app = b.Build();
app.UseAuthentication();
app.UseAuthorization();

var profiles = new Dictionary<string, ProfileDto>();

app.MapPut("/profile/me", async (ClaimsPrincipal user, IBus bus, ProfileDto dto) =>
{
    var uid = user.FindFirst("sub")?.Value ?? "anonymous";
    profiles[uid] = dto with { UserId = uid };
    await bus.Publish(new ProfileUpdated(uid, "Name", dto.Name ?? ""));
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

record ProfileDto(string? UserId, string? Name, string? City);
