using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Consumers;
using AutoVerdict.ProcessingService.Crawler;
using AutoVerdict.ProcessingService.Parsing;
using AutoVerdict.ProcessingService.Pipeline;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ProcessingOptions>(
    builder.Configuration.GetSection(ProcessingOptions.SectionName));

var testMode = builder.Configuration
    .GetSection(ProcessingOptions.SectionName)
    .GetValue<bool>(nameof(ProcessingOptions.TestMode));

if (testMode)
{
    builder.Services.AddSingleton<ICarCheckPipeline, FakeCarCheckPipeline>();
}
else
{
    builder.Services.Configure<CrawlerOptions>(
        builder.Configuration.GetSection(CrawlerOptions.SectionName));
    builder.Services.Configure<PlaywrightParserOptions>(
        builder.Configuration.GetSection(PlaywrightParserOptions.SectionName));
    builder.Services.AddSingleton<DomainRateLimiter>();
    builder.Services.AddSingleton<OtomotoListingParser>();
    builder.Services.AddSingleton<AiPipelineMetrics>();
    builder.Services.AddSingleton<AiStageRunner>();
    builder.Services.AddSingleton<EvidenceFormatter>();
    builder.Services.AddSingleton<FactExtractionStage>();
    builder.Services.AddSingleton<RiskAnalysisStage>();
    builder.Services.AddSingleton<ReportGenerationStage>();
    builder.Services.AddSingleton<ReportValidator>();
    builder.Services.AddSingleton<ReportRepairStage>();
    builder.Services.AddSingleton<CarCheckAnalysisPipeline>();
    builder.Services.AddSingleton<ICarCheckPipeline>(
        sp => sp.GetRequiredService<CarCheckAnalysisPipeline>());
}
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "autoverdict-processing-service",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
    .WithMetrics(metrics => metrics
        .AddMeter(AiPipelineMetrics.MeterName)
        .AddOtlpExporter());

builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();
