using Hangfire;
using Monivus.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

//builder.AddSqlServerDbContext<SampleDbContext>(connectionName: "sampleDb");

GlobalConfiguration.Configuration.UseInMemoryStorage();

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();
});

builder.Services.AddHangfireServer();

// Disable built-in health checks to use custom ones
builder.AddMongoDBClient(connectionName: "mongodb", configureSettings => { configureSettings.DisableHealthChecks = true; });

builder.Services.AddHealthChecks()
    .AddMongoDbEntry()
    .AddHangfireEntry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMonivusHealthChecks();

app.Run();