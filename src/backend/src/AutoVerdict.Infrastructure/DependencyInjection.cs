using AutoVerdict.Application.AI;
using AutoVerdict.Application.Checks;
using AutoVerdict.Application.Storage;
using AutoVerdict.Infrastructure.AI;
using AutoVerdict.Infrastructure.Checks;
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

        return services;
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

