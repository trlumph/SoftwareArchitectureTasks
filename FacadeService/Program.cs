var builder = WebApplication.CreateBuilder(args);

// Configure HTTP clients
builder.Services.AddHttpClient("LoggingService", client => client.BaseAddress = new Uri("http://localhost:5064/"));
builder.Services.AddHttpClient("MessagesService", client => client.BaseAddress = new Uri("http://localhost:5015/"));

var app = builder.Build();

app.MapPost("facade", async (IHttpClientFactory clientFactory, HttpContext httpContext) => {
    var request = await httpContext.Request.ReadFromJsonAsync<MessageRequest>();
    var loggingClient = clientFactory.CreateClient("LoggingService");
    var uuid = Guid.NewGuid().ToString();
    await loggingClient.PostAsJsonAsync("/log", new { Uuid = uuid, request?.Message });
    return Results.Ok(new { Uuid = uuid });
});

app.MapGet("facade", async (IHttpClientFactory clientFactory) => {
    var loggingClient = clientFactory.CreateClient("LoggingService");
    var messagesClient = clientFactory.CreateClient("MessagesService");
    var loggingResponse = await loggingClient.GetStringAsync("/log");
    var messagesResponse = await messagesClient.GetStringAsync("/message");
    return Results.Ok($"{loggingResponse}\n{messagesResponse}");
});

app.Run();

record MessageRequest(string Message);