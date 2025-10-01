using GracefulShutdownQueueWorker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var logPath = Environment.GetEnvironmentVariable("LOG_PATH") ?? "/app/logs/worker-.log";

builder.Services.AddSerilog((services, lc) => lc
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day));

builder.Services.AddHostedService<QueueWorker>();

var host = builder.Build();

host.Run();
