using Monivus.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "Monivus Exporter");
builder.Services.AddSystemd();

builder.Services.AddMonivusExporter(builder.Configuration);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });
});

var host = builder.Build();
await host.RunAsync();
