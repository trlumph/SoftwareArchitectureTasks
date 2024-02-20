using Consul;
using FacadeService;
using Hazelcast;
using ConsulManagerService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p =>
    new ConsulClient(consulConfig =>
    {
        consulConfig.Address = new Uri(builder.Configuration.GetValue<string>("ConsulConfig:Host")!);
    }));

var app = builder.Build();

var urls = builder.Configuration.GetValue<string>("ASPNETCORE_URLS")?.Split(';');
if (urls is null || urls.Length == 0)
{
    throw new Exception("ASPNETCORE_URLS environment variable is not set.");
}
var port = new Uri(urls.FirstOrDefault()!).Port;

var consulClient = app.Services.GetRequiredService<IConsulClient>();
var serviceId = "facade-service-"+Guid.NewGuid();
var registrationManager = app.Services.GetRequiredService<ConsulRegistrationManager>();
await registrationManager.Register(consulClient, "facade-service", serviceId, port);

app.Lifetime.ApplicationStopping.Register(async () =>
{
    await registrationManager.Deregister(consulClient, serviceId);
});

AppDomain.CurrentDomain.UnhandledException += async (sender, eventArgs) =>
{
    Console.WriteLine("Unhandled exception occurred. Deregistering service...");
    await registrationManager.Deregister(consulClient, serviceId);
};

app.UseSwagger();  
app.UseSwaggerUI();

app.MapPost("/facade", async (
    MessageRequest? request,
    IHazelcastClient hzClient,
    IHttpClientFactory httpClientFactory,
    MessageQueueConfigurationService mqConfigService) =>
{
    if (request is null) 
        return Results.BadRequest("Message is required");

    var queueName = await mqConfigService.GetMQNameAsync();
    var queue = await hzClient.GetQueueAsync<string>(queueName);
    await queue.OfferAsync(request.Message);

    var loggingServiceUri = await DiscoveryService.DiscoverServiceUri(consulClient, "logging-service");
    var client = httpClientFactory.CreateClient();
    var uuid = Guid.NewGuid().ToString();
    await client.PostAsJsonAsync(loggingServiceUri+"log", new { Uuid = uuid, request.Message });
    return Results.Ok(new { Uuid = uuid });
});

app.MapGet("/facade", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    
    var messageServiceUri = await DiscoveryService.DiscoverServiceUri(consulClient, "messages-service");
    Console.WriteLine("messageServiceUri" + messageServiceUri);
    var messagesResponse = await client.GetAsync(messageServiceUri + "messages");
    var messages = await messagesResponse.Content.ReadAsStringAsync();
    
    var loggingServiceUri = await DiscoveryService.DiscoverServiceUri(consulClient, "logging-service");
    var loggingResponse = await client.GetAsync(loggingServiceUri + "log");
    var loggedMessages = await loggingResponse.Content.ReadAsStringAsync();
    
    return Results.Ok($"{loggedMessages}\n{messages}");
});

app.Run();

record MessageRequest(string Message);