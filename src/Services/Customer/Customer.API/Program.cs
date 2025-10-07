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
// MassTransit + consumer
b.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserRegisteredConsumer>();

    x.UsingRabbitMq((context, bus) =>
    {
        bus.Host(cfg["Rabbit:Host"] ?? "localhost", "/", h =>
        {
            h.Username(cfg["Rabbit:User"] ?? "guest");
            h.Password(cfg["Rabbit:Pass"] ?? "guest");
        });

        // Let MassTransit create a queue for this consumer
        bus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("svc-customer", false));
    });
});

var app = b.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapPut("/profile/me", async (ClaimsPrincipal user, IBus bus, ProfileDto dto) =>
{
    var uid = user.FindFirst("sub")?.Value ?? "anonymous";
    UserRegisteredConsumer.Profiles[dto.UserId] = dto;
    await bus.Publish(new ProfileUpdated(uid, "Name", dto.Name ?? ""));
    return Results.NoContent();
}).RequireAuthorization();

// todo: dev only remove before deploy
app.MapGet("/debug/profiles", () => UserRegisteredConsumer.Profiles);

app.Run();
