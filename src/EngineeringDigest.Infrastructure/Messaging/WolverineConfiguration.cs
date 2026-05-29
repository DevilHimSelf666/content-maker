using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;

namespace EngineeringDigest.Infrastructure.Messaging;

public static class WolverineConfiguration
{
    public static void ConfigureEngineeringDigestWolverine(this WolverineOptions options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EngineeringDigest")
            ?? throw new InvalidOperationException("ConnectionStrings:EngineeringDigest is required.");

        options.Discovery.IncludeAssembly(typeof(VideoWorkflowHandlers).Assembly);
        options.PersistMessagesWithSqlServer(connectionString, "wolverine");
        options.UseEntityFrameworkCoreTransactions();
        options.Policies.UseDurableLocalQueues();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        options.Durability.InboxStaleTime = TimeSpan.FromMinutes(10);
        options.Durability.OutboxStaleTime = TimeSpan.FromMinutes(10);
        options.PublishFaultEvents();
        options.OnException<HttpRequestException>()
            .RetryWithCooldown(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
        options.OnException<TaskCanceledException>()
            .RetryWithCooldown(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
        options.OnException<InvalidOperationException>()
            .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
    }
}
