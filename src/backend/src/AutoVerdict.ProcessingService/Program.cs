using AutoVerdict.Infrastructure;
using AutoVerdict.ProcessingService.Consumers;
using AutoVerdict.ProcessingService.Crawler;
using AutoVerdict.ProcessingService.Parsing;
using AutoVerdict.ProcessingService.Pipeline;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);

var testMode = string.Equals(builder.Configuration["TEST_MODE"], "true", StringComparison.OrdinalIgnoreCase);

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
builder.Services.AddHostedService<CarCheckConsumer>();

var host = builder.Build();
host.Run();
