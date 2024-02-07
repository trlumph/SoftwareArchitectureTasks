using FacadeService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseSwagger();  
app.UseSwaggerUI();

app.MapPost("/facade", async (HttpContext httpContext, ILoggingClientService loggingClientService) => {
    var request = await httpContext.Request.ReadFromJsonAsync<MessageRequest>();
    var client = await loggingClientService.GetClientAsync();
    var uuid = Guid.NewGuid().ToString();
    await client.PostAsJsonAsync("/log", new { Uuid = uuid, Message = request?.Message });
    return Results.Ok(new { Uuid = uuid });
});

app.MapGet("/facade", async (ILoggingClientService loggingClientService) => {
    var client = await loggingClientService.GetClientAsync();
    var loggingResponse = await client.GetStringAsync("/log");
    var messagesResponse = "Static response or another service call";
    return Results.Ok($"{loggingResponse}\n{messagesResponse}");
});

app.Run();

record MessageRequest(string Message);