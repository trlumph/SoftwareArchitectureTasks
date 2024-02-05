using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var messages = new ConcurrentDictionary<string, string>();

app.MapPost("log", (LogEntry logEntry) => {
    messages[logEntry.Uuid] = logEntry.Message;
    Console.WriteLine(logEntry.Message);
    return Results.Ok();
});

app.MapGet("log", () => Results.Ok(string.Join("\n", messages.Values)));

app.Run();

record LogEntry(string Uuid, string Message);