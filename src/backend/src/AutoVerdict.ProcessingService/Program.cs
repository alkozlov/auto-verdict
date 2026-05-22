using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Configuration;
using AutoVerdict.ProcessingService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<NatsOptions>(opts =>
{
    opts.Url = builder.Configuration["NATS_URL"]
        ?? builder.Configuration["Nats:Url"]
        ?? "nats://localhost:4222";
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();
