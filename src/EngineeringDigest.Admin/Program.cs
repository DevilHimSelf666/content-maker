using System.Net;
using System.Text;
using EngineeringDigest.Admin.Components;
using EngineeringDigest.Admin.Security;
using EngineeringDigest.Infrastructure.DependencyInjection;
using EngineeringDigest.Infrastructure.Messaging;
using EngineeringDigest.Infrastructure.Observability;
using EngineeringDigest.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddEngineeringDigestInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(HeaderRoleAuthenticationHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, HeaderRoleAuthenticationHandler>(HeaderRoleAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReview", policy => policy.RequireRole("Reviewer", "Administrator"));
    options.AddPolicy("CanPublish", policy => policy.RequireRole("Publisher", "Administrator"));
    options.AddPolicy("CanAdmin", policy => policy.RequireRole("Administrator"));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddMeter(EngineeringDigestMetrics.MeterName));

builder.Host.UseWolverine(options => options.ConfigureEngineeringDigestWolverine(builder.Configuration));

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var header) ? header.ToString() : context.TraceIdentifier;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    using (Serilog.Context.LogContext.PushProperty("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString()))
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next();
    }
});
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapGet("/knowledge/search", async (string? q, Guid? categoryId, bool semantic, IKnowledgeService knowledge, CancellationToken cancellationToken) =>
    await knowledge.SearchAsync(new KnowledgeSearchRequest(q, semantic, categoryId, [], 25), cancellationToken));

app.MapGet("/knowledge/article/{id:guid}", async (Guid id, KnowledgeDbContext db, CancellationToken cancellationToken) =>
{
    var article = await db.KnowledgeArticles.AsNoTracking()
        .Include(x => x.Category)
        .Include(x => x.Tags).ThenInclude(x => x.KnowledgeTag)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    return article is null ? Results.NotFound() : Results.Ok(article);
});

app.MapGet("/knowledge/related/{id:guid}", async (Guid id, KnowledgeDbContext db, CancellationToken cancellationToken) =>
{
    var related = await db.KnowledgeArticleRelations.AsNoTracking()
        .Include(x => x.RelatedArticle)
        .Where(x => x.SourceArticleId == id)
        .OrderByDescending(x => x.Score)
        .Select(x => new { x.RelatedArticleId, x.RelatedArticle!.Title, x.Score, x.Reason })
        .ToListAsync(cancellationToken);
    return Results.Ok(related);
});

app.MapPost("/knowledge/ask", async (AskKnowledgeRequest request, IRagService rag, CancellationToken cancellationToken) =>
    await rag.AnswerAsync(request.Question, cancellationToken));

app.MapGet("/knowledge/article/{id:guid}/export/{format}", async (Guid id, string format, KnowledgeDbContext db, CancellationToken cancellationToken) =>
{
    var article = await db.KnowledgeArticles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (article is null)
    {
        return Results.NotFound();
    }

    return format.ToLowerInvariant() switch
    {
        "markdown" or "md" => Results.Text($"# {article.Title}\n\n{article.BodyMarkdown}", "text/markdown", Encoding.UTF8),
        "html" => Results.Text($"<article><h1>{WebUtility.HtmlEncode(article.Title)}</h1><pre>{WebUtility.HtmlEncode(article.BodyMarkdown)}</pre></article>", "text/html", Encoding.UTF8),
        "pdf" => Results.Text("PDF export is queued for the rendering service; use Markdown or HTML export in MVP.", "text/plain", Encoding.UTF8),
        _ => Results.BadRequest("Supported formats: markdown, html, pdf")
    };
});

app.MapGet("/knowledge/learning-path/{id:guid}/export/{format}", async (Guid id, string format, KnowledgeDbContext db, CancellationToken cancellationToken) =>
{
    var path = await db.LearningPaths.AsNoTracking()
        .Include(x => x.Articles).ThenInclude(x => x.KnowledgeArticle)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (path is null)
    {
        return Results.NotFound();
    }

    var markdown = new StringBuilder().AppendLine($"# {path.Name}").AppendLine();
    foreach (var item in path.Articles.OrderBy(x => x.Order))
    {
        markdown.AppendLine($"## {item.Order}. {item.KnowledgeArticle?.Title}");
        markdown.AppendLine(item.KnowledgeArticle?.BodyMarkdown);
        markdown.AppendLine();
    }

    return format.ToLowerInvariant() switch
    {
        "markdown" or "md" => Results.Text(markdown.ToString(), "text/markdown", Encoding.UTF8),
        "html" => Results.Text($"<pre>{WebUtility.HtmlEncode(markdown.ToString())}</pre>", "text/html", Encoding.UTF8),
        "pdf" => Results.Text("PDF collection export is queued for the rendering service; use Markdown or HTML export in MVP.", "text/plain", Encoding.UTF8),
        _ => Results.BadRequest("Supported formats: markdown, html, pdf")
    };
});


using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(CancellationToken.None);
}

await app.RunAsync();

public sealed record AskKnowledgeRequest(string Question);
