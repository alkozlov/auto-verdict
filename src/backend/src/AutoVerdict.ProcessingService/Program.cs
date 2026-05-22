using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Configuration;
using AutoVerdict.ProcessingService.Consumers;
using AutoVerdict.ProcessingService.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<NatsOptions>(opts =>
{
    opts.Url = builder.Configuration["NATS_URL"]
        ?? builder.Configuration["Nats:Url"]
        ?? "nats://localhost:4222";
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CarCheckAnalysisPipeline>();
builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();
