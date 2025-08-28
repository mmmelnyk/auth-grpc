using Common.Contracts;
using Common.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Customer.Grpc;

var b = WebApplication.CreateBuilder(args);
var cfg = b.Configuration;

b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearer(o => o.RequireHttpsMetadata = false);
b.Services.AddAuthorization();

// register a typed gRPC client for CustomerProfile
b.Services.AddGrpcClient<CustomerProfile.CustomerProfileClient>(o =>
{
    // The gRPC server (Customer.Grpc) is running on localhost:5003
    o.Address = new Uri("http://localhost:5003");
});

var app = b.Build();
app.UseAuthentication();
app.UseAuthorization();

// Example: expose a test endpoint in Auth.API that calls Customer.Grpc
app.MapGet("/auth/test-profile/{id}", async (string id, CustomerProfile.CustomerProfileClient client) =>
{
    var res = await client.GetProfileAsync(new GetProfileRequest { UserId = id });
    return Results.Ok(new { res.UserId, res.Name, res.City });
});

app.Run();
