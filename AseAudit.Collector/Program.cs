using AseAudit.Collector;

var builder = Host.CreateApplicationBuilder(args);

// Register services for dependency injection
builder.Services.AddSingleton<IScriptExecutor, PowerShellExecutor>();
builder.Services.AddSingleton<ScriptEngine>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
