using System.Text;
using Consul;
using Hazelcast;
using ConsulManagerService;


var builder = WebApplication.CreateBuilder(args);

var consulAddress = new Uri(builder.Configuration.GetValue<string>("ConsulConfig:Host")!);

builder.Services.AddSingleton<IConsulClient, ConsulClient>(p =>
    new ConsulClient(consulConfig =>
    {
        consulConfig.Address = consulAddress;
    }));

builder.Services.AddSingleton<ConsulRegistrationManager>();
builder.Services.AddSingleton<HazelcastConfigurationService>();

var app = builder.Build();

var consulClient = app.Services.GetRequiredService<IConsulClient>();
var hazelcastConfigService = app.Services.GetRequiredService<HazelcastConfigurationService>();
var selectedNode = await hazelcastConfigService.GetHazelcastNodeAsync();

var hzOptions = new HazelcastOptionsBuilder()
    .With((configuration, options) => 
    {
        options.Networking.Addresses.Add(selectedNode);
    })
    .Build();

var hzClient = await HazelcastClientFactory.StartNewClientAsync(hzOptions);
app.Logger.LogInformation("Connected to Hazelcast node: {SelectedNode}", selectedNode);


var serviceName = "logging-service";
var serviceId = $"{serviceName}-{Guid.NewGuid()}";
var servicePort = new Uri(builder.Configuration.GetValue<string>("ASPNETCORE_URLS")!).Port;

var registrationManager = app.Services.GetRequiredService<ConsulRegistrationManager>();
await registrationManager.Register(consulClient, serviceName, serviceId, servicePort);

app.Lifetime.ApplicationStopping.Register(async () =>
{
    await registrationManager.Deregister(consulClient, serviceId);
    await hzClient.DisposeAsync();
});

AppDomain.CurrentDomain.UnhandledException += async (sender, eventArgs) =>
{
    Console.WriteLine("Unhandled exception occurred. Deregistering service...");
    await registrationManager.Deregister(consulClient, serviceId);
};

app.MapPost("log", async (LogEntry logEntry) => {
    var map = await hzClient.GetMapAsync<string, string>("messages");
    await map.SetAsync(logEntry.Uuid, logEntry.Message);
    Console.WriteLine(logEntry.Message);
    return Results.Ok();
});

app.MapGet("log", async () => {
    var map = await hzClient.GetMapAsync<string, string>("messages");
    var allMessages = await map.GetValuesAsync();
    return Results.Ok(string.Join("\n", allMessages));
});


app.Run();

record LogEntry(string Uuid, string Message);