using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Consumers;
using AutoVerdict.ProcessingService.Parsing;
using AutoVerdict.ProcessingService.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<PlaywrightParserOptions>(
    builder.Configuration.GetSection(PlaywrightParserOptions.SectionName));
builder.Services.AddSingleton<OtomotoListingParser>();
builder.Services.AddSingleton<CarCheckAnalysisPipeline>();
builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();
