using AutoVerdict.Application.AI;
using AutoVerdict.Application.Auth;
using AutoVerdict.Application.Checks;
using AutoVerdict.Application.Storage;
using AutoVerdict.Infrastructure.AI;
using AutoVerdict.Infrastructure.Auth;
using AutoVerdict.Infrastructure.Checks;
using AutoVerdict.Infrastructure.Messaging;
using AutoVerdict.Infrastructure.Persistence;
using AutoVerdict.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<ICarCheckService, CarCheckService>();
        services.AddScoped<ICarCheckResultService, CarCheckResultService>();

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
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? configuration["Database:ConnectionString"]
            ?? configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string is not configured. Set DATABASE_URL or ConnectionStrings:Postgres.");
        }

        return connectionString;
    }
}

