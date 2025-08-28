using System.Collections.Concurrent;
using Grpc.Core;

namespace Customer.Grpc;

// simple in-memory store for dev
public class ProfilesStore
{
    public ConcurrentDictionary<string, (string name, string city)> Data { get; } = new();
}

public class CustomerProfileService : CustomerProfile.CustomerProfileBase
{
    private readonly ProfilesStore _store;

    public CustomerProfileService(ProfilesStore store) => _store = store;

    public override Task<GetProfileResponse> GetProfile(GetProfileRequest request, ServerCallContext context)
    {
        _store.Data.TryGetValue(request.UserId, out var p);
        return Task.FromResult(new GetProfileResponse
        {
            UserId = request.UserId,
            Name = p.name ?? string.Empty,
            City = p.city ?? string.Empty
        });
    }

    public override Task<UpsertProfileResponse> UpsertProfile(UpsertProfileRequest request, ServerCallContext context)
    {
        _store.Data[request.UserId] = (request.Name, request.City);
        return Task.FromResult(new UpsertProfileResponse { Ok = true });
    }
}
