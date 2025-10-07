using Common.Contracts;
using MassTransit;

public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    // for now, same in-memory store you used for gRPC service
    public static Dictionary<string, ProfileDto> Profiles = new();

    public Task Consume(ConsumeContext<UserRegistered> ctx)
    {
        var id = ctx.Message.UserId;
        Profiles.TryAdd(id, new ProfileDto(id, null, null)); // skeleton profile
        Console.WriteLine($"[Customer] Initialized profile for {id}");
        return Task.CompletedTask;
    }
}

public record ProfileDto(string? UserId, string? Name, string? City);
