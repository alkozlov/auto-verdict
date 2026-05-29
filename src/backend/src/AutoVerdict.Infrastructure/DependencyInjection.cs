using System.Net.Http.Headers;
using AutoVerdict.Application.AI;
using AutoVerdict.Application.Auth;
using AutoVerdict.Application.Checks;
using AutoVerdict.Application.Payments;
using AutoVerdict.Application.Storage;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Infrastructure.AI;
using AutoVerdict.Infrastructure.Auth;
using AutoVerdict.Infrastructure.Checks;
using AutoVerdict.Infrastructure.Messaging;
using AutoVerdict.Infrastructure.Payments;
using AutoVerdict.Infrastructure.Persistence;
using AutoVerdict.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = GetPostgresConnectionString(configuration);

        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = connectionString;
        });

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.Configure<ClaudeOptions>(opts =>
        {
            configuration.GetSection(ClaudeOptions.SectionName).Bind(opts);
            if (configuration["CLAUDE_API_KEY"] is { Length: > 0 } key)
                opts.ApiKey = key;
            if (configuration["CLAUDE_MODEL"] is { Length: > 0 } model)
                opts.Model = model;
        });
        var configuredClaudeModel =
            configuration["CLAUDE_MODEL"]
            ?? configuration["Claude:Model"]
            ?? "claude-haiku-4-5";

        services.Configure<AiPipelineOptions>(opts =>
        {
            configuration.GetSection(AiPipelineOptions.SectionName).Bind(opts);
            if (configuration["AI_DEFAULT_BUDGET_EUR"] is { Length: > 0 } defaultBudget
                && decimal.TryParse(defaultBudget, out var parsedDefaultBudget))
                opts.DefaultBudgetEur = parsedDefaultBudget;
            if (configuration["AI_HARD_BUDGET_EUR"] is { Length: > 0 } hardBudget
                && decimal.TryParse(hardBudget, out var parsedHardBudget))
                opts.HardBudgetEur = parsedHardBudget;

            EnsureStage(opts, "FactExtraction", configuration["AI_FACT_EXTRACTION_MODEL"] ?? configuredClaudeModel, 2500, true);
            EnsureStage(opts, "RiskAnalysis", configuration["AI_RISK_ANALYSIS_MODEL"] ?? configuredClaudeModel, 5000, true);
            EnsureStage(opts, "ReportGeneration", configuration["AI_REPORT_GENERATION_MODEL"] ?? configuredClaudeModel, 8000, true);
            EnsureStage(opts, "ReportRepair", configuration["AI_REPORT_REPAIR_MODEL"] ?? configuredClaudeModel, 8000, true);
            EnsureStage(opts, "OpusReview", configuration["AI_OPUS_REVIEW_MODEL"] ?? "claude-opus-4-1", 3000, false);

            OverrideStageModel(opts, "FactExtraction", configuration["AI_FACT_EXTRACTION_MODEL"]);
            OverrideStageModel(opts, "RiskAnalysis", configuration["AI_RISK_ANALYSIS_MODEL"]);
            OverrideStageModel(opts, "ReportGeneration", configuration["AI_REPORT_GENERATION_MODEL"]);
            OverrideStageModel(opts, "ReportRepair", configuration["AI_REPORT_REPAIR_MODEL"]);
            OverrideStageModel(opts, "OpusReview", configuration["AI_OPUS_REVIEW_MODEL"]);
            if (configuration["AI_OPUS_REVIEW_ENABLED"] is { Length: > 0 } opusEnabled
                && bool.TryParse(opusEnabled, out var parsedOpusEnabled)
                && opts.Stages.TryGetValue("OpusReview", out var opusStage))
                opusStage.Enabled = parsedOpusEnabled;
        });
        services.Configure<AiPricingOptions>(opts =>
        {
            configuration.GetSection(AiPricingOptions.SectionName).Bind(opts);
            if (configuration["AI_USD_TO_EUR_RATE"] is { Length: > 0 } rate
                && decimal.TryParse(rate, out var parsedRate))
                opts.UsdToEurRate = parsedRate;
        });
        services.AddSingleton<IAiClient, ClaudeAiClient>();
        services.AddSingleton<IAiAnalysisProvider, ClaudeAiAnalysisProvider>();

        services.Configure<S3Options>(opts =>
        {
            configuration.GetSection(S3Options.SectionName).Bind(opts);
            if (configuration["S3_ENDPOINT"] is { Length: > 0 } endpoint)
                opts.Endpoint = endpoint;
            if (configuration["S3_ACCESS_KEY"] is { Length: > 0 } accessKey)
                opts.AccessKey = accessKey;
            if (configuration["S3_SECRET_KEY"] is { Length: > 0 } secretKey)
                opts.SecretKey = secretKey;
            if (configuration["S3_BUCKET"] is { Length: > 0 } bucket)
                opts.Bucket = bucket;
        });
        services.AddSingleton<IDocumentStorageClient, S3DocumentStorageClient>();

        services.Configure<WhitelistOptions>(opts =>
        {
            configuration.GetSection(WhitelistOptions.SectionName).Bind(opts);
            if (configuration["WHITELIST_EMAILS"] is { Length: > 0 } emails)
                opts.Emails = emails;
        });

        services.AddScoped<ICarCheckService, CarCheckService>();
        services.AddScoped<ICarCheckResultService, CarCheckResultService>();

        services.Configure<PaymentOptions>(opts =>
        {
            configuration.GetSection(PaymentOptions.SectionName).Bind(opts);
            if (configuration["BILLING_PROVIDER"] is { Length: > 0 } p) opts.Provider = p;
            if (configuration["BILLING_API_KEY"] is { Length: > 0 } k) opts.ApiKey = k;
            if (configuration["BILLING_WEBHOOK_SECRET"] is { Length: > 0 } s) opts.WebhookSecret = s;
            if (configuration["BILLING_STORE_ID"] is { Length: > 0 } id) opts.StoreId = id;
            if (configuration["BILLING_SINGLE_PRODUCT"] is { Length: > 0 } v1)
                opts.PackageVariantIds["credits_1"] = v1;
            if (configuration["BILLING_PACK_3"] is { Length: > 0 } v3)
                opts.PackageVariantIds["credits_3"] = v3;
        });

        services.AddHttpClient("lemonsqueezy", (sp, client) =>
        {
            var paymentOpts = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
            client.BaseAddress = new Uri("https://api.lemonsqueezy.com/v1/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.api+json");
            if (!string.IsNullOrEmpty(paymentOpts.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", paymentOpts.ApiKey);
        });

        var paymentProvider = configuration["BILLING_PROVIDER"]
            ?? configuration["Payment:Provider"]
            ?? "mock";
        if (paymentProvider.Equals("lemonsqueezy", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentService, LemonSqueezyPaymentService>();
        else
            services.AddScoped<IPaymentService, MockPaymentService>();

        services.Configure<NatsOptions>(opts =>
        {
            configuration.GetSection(NatsOptions.SectionName).Bind(opts);
            if (configuration["NATS_URL"] is { Length: > 0 } url)
                opts.Url = url;
        });

        services.Configure<AuthOptions>(opts =>
        {
            configuration.GetSection(AuthOptions.SectionName).Bind(opts);
            if (configuration["JWT_SECRET"] is { Length: > 0 } s) opts.JwtSecret = s;
            if (configuration["GOOGLE_CLIENT_ID"] is { Length: > 0 } id) opts.GoogleClientId = id;
            if (configuration["GOOGLE_CLIENT_SECRET"] is { Length: > 0 } cs) opts.GoogleClientSecret = cs;
        });

        services.AddSingleton<JwtService>();
        services.AddScoped<IUserAuthService, UserAuthService>();

        return services;
    }

    public static IServiceCollection AddOutboxPublisher(this IServiceCollection services)
    {
        services.AddHostedService<OutboxPublisherService>();
        return services;
    }

    public static AuthOptions GetAuthOptions(IConfiguration configuration)
    {
        var opts = new AuthOptions();
        configuration.GetSection(AuthOptions.SectionName).Bind(opts);
        if (configuration["JWT_SECRET"] is { Length: > 0 } s) opts.JwtSecret = s;
        if (configuration["GOOGLE_CLIENT_ID"] is { Length: > 0 } id) opts.GoogleClientId = id;
        if (configuration["GOOGLE_CLIENT_SECRET"] is { Length: > 0 } cs) opts.GoogleClientSecret = cs;
        return opts;
    }

    private static string GetPostgresConnectionString(IConfiguration configuration)
    {
        // DATABASE_URL env var takes priority so Docker deployments always win
        // over any appsettings.Development.json local-dev defaults.
        var connectionString =
            configuration["DATABASE_URL"]
            ?? configuration.GetConnectionString("Postgres")
            ?? configuration["Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string is not configured. Set DATABASE_URL or ConnectionStrings:Postgres.");
        }

        return connectionString;
    }

    private static void EnsureStage(
        AiPipelineOptions options,
        string name,
        string model,
        int maxTokens,
        bool enabled)
    {
        if (!options.Stages.TryGetValue(name, out var stage))
        {
            options.Stages[name] = new AiStageOptions
            {
                Model = model,
                MaxTokens = maxTokens,
                Enabled = enabled,
            };
            return;
        }

        if (string.IsNullOrWhiteSpace(stage.Model))
            stage.Model = model;
        if (stage.MaxTokens <= 0)
            stage.MaxTokens = maxTokens;
    }

    private static void OverrideStageModel(AiPipelineOptions options, string name, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return;
        if (options.Stages.TryGetValue(name, out var stage))
            stage.Model = model;
    }
}
