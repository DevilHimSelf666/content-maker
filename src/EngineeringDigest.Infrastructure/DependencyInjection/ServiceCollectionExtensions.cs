using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Knowledge;
using EngineeringDigest.Infrastructure.Knowledge;
using EngineeringDigest.Infrastructure.Llm;
using EngineeringDigest.Infrastructure.Persistence;
using EngineeringDigest.Infrastructure.Telegram;
using EngineeringDigest.Infrastructure.Transcripts;
using EngineeringDigest.Infrastructure.YouTube;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringDigest.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEngineeringDigestInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.Configure<LlmOptions>(configuration.GetSection("Llm"));
        services.Configure<TranscriptOptions>(configuration.GetSection("Transcript"));
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
        services.Configure<EmbeddingOptions>(configuration.GetSection("Embedding"));

        var connectionString = configuration.GetConnectionString("EngineeringDigest")
            ?? throw new InvalidOperationException("ConnectionStrings:EngineeringDigest is required.");

        services.AddDbContext<EngineeringDigestDbContext>(options =>
            options.UseSqlServer(connectionString));

        var knowledgeConnectionString = configuration.GetConnectionString("Knowledge")
            ?? throw new InvalidOperationException("ConnectionStrings:Knowledge is required.");

        services.AddDbContext<KnowledgeDbContext>(options =>
            options.UseNpgsql(knowledgeConnectionString));

        services.AddScoped<DbInitializer>();
        services.AddHttpClient<IYouTubeRssClient, YouTubeRssClient>();
        services.AddHttpClient<ITranscriptClient, TranscriptClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TranscriptOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });
        services.AddHttpClient<ILlmClient, OpenAiCompatibleLlmClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });
        services.AddHttpClient<IEmbeddingProvider, OpenAiCompatibleEmbeddingProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IRagService, RagService>();
        services.AddHttpClient<ITelegramPublisher, TelegramPublisher>();

        return services;
    }
}
