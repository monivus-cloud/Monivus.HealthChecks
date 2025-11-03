using Monivus.UI.Components;
using Monivus.HealthChecks;
using Monivus.HealthChecks.Exporter;
using Monivus.HealthChecks.Redis;
using Monivus.HealthChecks.Url;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddRedisDistributedCache(connectionName: "cache",
    settings => 
    { 
        settings.DisableHealthChecks = true;
    },
    options =>
    {
        options.AllowAdmin = true;
    });

builder.Services.AddHealthChecks()
    .AddResourceUtilizationEntry()
    .AddRedisEntry()
    .AddUrlEntry("https://www.google.com/");

builder.Services.AddMonivusExporter(configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Aggregate remote health endpoints into UI health output
app.UseMonivusAggregatedHealthChecks(opts =>
{
    // Example: add API health endpoint
    opts.AddEndpoint("https://localhost:7048/health", "api");
    // You can add more:
    // opts.AddEndpoint("jobs", "https://localhost:5002/health");
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
