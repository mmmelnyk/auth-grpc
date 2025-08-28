using Grpc.Core;
using Auth.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc();

// Register your token validator (stub for now)
builder.Services.AddSingleton<TokenValidator>();

// enable reflection in dev only
if (builder.Environment.IsDevelopment())
    builder.Services.AddGrpcReflection();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService(); 
// Map the gRPC implementation
app.MapGrpcService<AuthValidationService>();

// Optional: simple root endpoint so you can curl it
app.MapGet("/", () => "Auth gRPC service is running");

app.Run();


// === Helpers & Service Implementation ===

public class TokenValidator
{
    // Super simple fake validation (replace with real JWT later)
    public (bool valid, string? userId) Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return (false, null);
        if (token.StartsWith("fake-jwt-for-"))
            return (true, token.Replace("fake-jwt-for-", ""));
        return (false, null);
    }
}

public class AuthValidationService : Auth.Grpc.AuthValidation.AuthValidationBase
{
    private readonly TokenValidator _validator;
    public AuthValidationService(TokenValidator validator) => _validator = validator;

    public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest req, ServerCallContext context)
    {
        var (ok, uid) = _validator.Validate(req.AccessToken);
        return Task.FromResult(new ValidateTokenResponse
        {
            Valid = ok,
            UserId = uid ?? ""
        });
    }

    public override Task<GetUserClaimsResponse> GetUserClaims(GetUserClaimsRequest req, ServerCallContext context)
    {
        var res = new GetUserClaimsResponse();
        res.Claims.Add("sub", req.UserId);
        res.Claims.Add("role", "user"); // hardcoded example
        return Task.FromResult(res);
    }
}
