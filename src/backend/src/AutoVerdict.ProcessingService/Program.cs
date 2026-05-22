using AutoVerdict.ProcessingService;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
