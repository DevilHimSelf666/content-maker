using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Infrastructure.Articles;
using EngineeringDigest.Infrastructure.Health;
using EngineeringDigest.Infrastructure.Llm;
using EngineeringDigest.Infrastructure.Persistence;
using EngineeringDigest.Infrastructure.Prompts;
using EngineeringDigest.Infrastructure.Observability;
using EngineeringDigest.Infrastructure.Telegram;
using EngineeringDigest.Infrastructure.Transcripts;
using EngineeringDigest.Infrastructure.YouTube;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace EngineeringDigest.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEngineeringDigestInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.Configure<LlmOptions>(configuration.GetSection("Llm"));
        services.Configure<TranscriptOptions>(configuration.GetSection("Transcript"));
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));

        var connectionString = configuration.GetConnectionString("EngineeringDigest")
            ?? throw new InvalidOperationException("ConnectionStrings:EngineeringDigest is required.");

        services.AddDbContext<EngineeringDigestDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<DbInitializer>();
        services.AddSingleton<EngineeringDigestMetrics>();
        services.AddScoped<IPromptTemplateProvider, PromptTemplateProvider>();
        services.AddScoped<IArticleQualityScorer, LlmArticleQualityScorer>();
        services.AddHttpClient<IYouTubeRssClient, YouTubeRssClient>()
            .AddPolicyHandler(GetRetryPolicy());
        services.AddHttpClient<ITranscriptClient, TranscriptClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TranscriptOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        }).AddPolicyHandler(GetRetryPolicy());
        services.AddHttpClient<ILlmClient, OpenAiCompatibleLlmClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        }).AddPolicyHandler(GetRetryPolicy());
        services.AddHttpClient<ITelegramPublisher, TelegramPublisher>()
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<TranscriptServiceHealthCheck>();
        services.AddHttpClient<LlmProviderHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sqlserver")
            .AddCheck<TranscriptServiceHealthCheck>("transcript-service")
            .AddCheck<LlmProviderHealthCheck>("llm-provider");

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
