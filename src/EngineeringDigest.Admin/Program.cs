using EngineeringDigest.Admin.Components;
using EngineeringDigest.Infrastructure.DependencyInjection;
using EngineeringDigest.Infrastructure.Messaging;
using EngineeringDigest.Infrastructure.Persistence;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddEngineeringDigestInfrastructure(builder.Configuration);

builder.Host.UseWolverine(options =>
{
    options.Discovery.IncludeAssembly(typeof(VideoWorkflowHandlers).Assembly);
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(CancellationToken.None);
}

await app.RunAsync();
