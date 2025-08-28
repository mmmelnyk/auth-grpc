using Customer.Grpc;
using Grpc.AspNetCore.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton<ProfilesStore>();

if (builder.Environment.IsDevelopment())
    builder.Services.AddGrpcReflection();

var app = builder.Build();
app.MapGrpcService<CustomerProfileService>();

if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

app.MapGet("/", () => "Customer gRPC running");
app.Run();
