using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppBus(this IServiceCollection services, IConfiguration cfg, string servicePrefix)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingRabbitMq((ctx, bus) =>
            {
                bus.Host(cfg["Rabbit:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(cfg["Rabbit:User"] ?? "guest");
                    h.Password(cfg["Rabbit:Pass"] ?? "guest");
                });
                bus.ConfigureEndpoints(ctx, new KebabCaseEndpointNameFormatter(servicePrefix, false));
            });
        });
        return services;
    }
}
