using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Consumers;
using AutoVerdict.ProcessingService.Parsing;
using AutoVerdict.ProcessingService.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// NatsOptions, OutboxPublisherService and all infrastructure services
// are registered inside AddInfrastructure.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<PlaywrightParserOptions>(opts =>
{
    builder.Configuration.GetSection(PlaywrightParserOptions.SectionName).Bind(opts);
    if (TryGetBool(builder.Configuration["PLAYWRIGHT_HEADLESS"], out var headless))
        opts.Headless = headless;
    if (TryGetBool(builder.Configuration["PLAYWRIGHT_DEVTOOLS"], out var devtools))
        opts.Devtools = devtools;
    if (int.TryParse(builder.Configuration["PLAYWRIGHT_SLOW_MO_MS"], out var slowMoMs))
        opts.SlowMoMs = slowMoMs;
    if (int.TryParse(builder.Configuration["PLAYWRIGHT_DEBUG_PAUSE_MS"], out var debugPauseMs))
        opts.DebugPauseMs = debugPauseMs;
});
builder.Services.AddSingleton<ICarListingParser, OtomotoListingParser>();
builder.Services.AddSingleton<CarCheckAnalysisPipeline>();
builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();

static bool TryGetBool(string? value, out bool parsed)
{
    parsed = false;
    return !string.IsNullOrWhiteSpace(value)
        && bool.TryParse(value, out parsed);
}
