var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("message", () => "not implemented yet");

app.Run();