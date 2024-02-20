using System.Reflection;
using Consul;
using ConsulManagerService;
using FacadeService;
using Hazelcast;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceRegister
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection serviceCollection)
    {
        var serviceType = typeof(Service);
        var definedTypes = serviceType.Assembly.DefinedTypes;

        var services = definedTypes
            .Where(x => x.GetTypeInfo().GetCustomAttribute<Service>() != null);

        foreach (var service in services)
        {
            serviceCollection.AddTransient(service);
        }
        
        serviceCollection.AddSingleton<IHazelcastClient>(serviceProvider =>
        {
            var hzOptions = new HazelcastOptionsBuilder().Build();
            return HazelcastClientFactory.StartNewClientAsync(hzOptions).GetAwaiter().GetResult();
        });

        serviceCollection.AddSingleton<ConsulRegistrationManager>();
        serviceCollection.AddSingleton<MessageQueueConfigurationService>();
        
        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddSwaggerGen();
        serviceCollection.AddHttpClient();
        
        return serviceCollection;
    }
}