using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Consumers;
using AutoVerdict.ProcessingService.Parsing;
using AutoVerdict.ProcessingService.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// NatsOptions, OutboxPublisherService and all infrastructure services
// are registered inside AddInfrastructure.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICarListingParser, OtomotoListingParser>();
builder.Services.AddSingleton<CarCheckAnalysisPipeline>();
builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();
