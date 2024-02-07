using System.Reflection;
using FacadeService;

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
        
        serviceCollection.AddHttpClient("LoggingService1", client => client.BaseAddress = new Uri("http://localhost:5064/"));
        serviceCollection.AddHttpClient("LoggingService2", client => client.BaseAddress = new Uri("http://localhost:5065/"));
        serviceCollection.AddHttpClient("LoggingService3", client => client.BaseAddress = new Uri("http://localhost:5066/"));

        serviceCollection.AddSingleton<ILoggingClientService, LoggingClientService>();

        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddSwaggerGen();
        
        return serviceCollection;
    }
}