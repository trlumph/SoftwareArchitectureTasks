using System.Text;
using Consul;
using ConsulManagerService;
using Hazelcast;
using MessagesService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHazelcastClient>(serviceProvider =>
{
    var hzOptions = new HazelcastOptionsBuilder().Build();
    return HazelcastClientFactory.StartNewClientAsync(hzOptions).GetAwaiter().GetResult();
});

var consulAddress = new Uri(builder.Configuration.GetValue<string>("ConsulConfig:Host")!);

builder.Services.AddSingleton<IConsulClient, ConsulClient>(p =>
    new ConsulClient(consulConfig =>
    {
        consulConfig.Address = consulAddress;
    }));

builder.Services.AddSingleton<IMessagesManager, MessagesManager>();
builder.Services.AddSingleton<HazelcastConfigurationService>();
builder.Services.AddSingleton<ConfigServer>();
builder.Services.AddSingleton<MessageQueueConfigurationService>();
builder.Services.AddSingleton<ConsulRegistrationManager>();

var app = builder.Build();

var messagesService = app.Services.GetRequiredService<IMessagesManager>();
var consulClient = app.Services.GetRequiredService<IConsulClient>();
var configServer = app.Services.GetRequiredService<ConfigServer>();

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

// Start listening to the message queue in the background
var backgroundTaskCTS = new CancellationTokenSource();
var listeningTask = messagesService.StartListening(backgroundTaskCTS.Token);

var serviceName = "messages-service";
var serviceId = $"{serviceName}-{Guid.NewGuid()}";
var servicePort = new Uri(builder.Configuration.GetValue<string>("ASPNETCORE_URLS")!).Port;

var registrationManager = app.Services.GetRequiredService<ConsulRegistrationManager>();
await registrationManager.Register(consulClient, serviceName, serviceId, servicePort);

app.Lifetime.ApplicationStopping.Register(async () =>
{
    backgroundTaskCTS.Cancel();
    await registrationManager.Deregister(consulClient, serviceId);
    await hzClient.DisposeAsync();
});

AppDomain.CurrentDomain.UnhandledException += async (sender, eventArgs) =>
{
    Console.WriteLine("Unhandled exception occurred. Deregistering service...");
    backgroundTaskCTS.Cancel();
    await registrationManager.Deregister(consulClient, serviceId);
    await hzClient.DisposeAsync();
};

app.MapGet("messages", async (IMessagesManager messagesManager) =>
{
    var messages = await messagesManager.GetAllMessagesAsync();
    return Results.Ok(string.Join("\n", messages));
});

app.Run();