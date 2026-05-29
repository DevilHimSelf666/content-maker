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

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(CancellationToken.None);
}

await app.RunAsync();
