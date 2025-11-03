var builder = DistributedApplication.CreateBuilder(args);

var cache = builder
    .AddRedis("cache")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var sqlServer = builder
    .AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var sampleDb = sqlServer.AddDatabase("sampleDb");

var api = builder.AddProject<Projects.Monivus_Api>("api")
    .WithReference(sampleDb)
    .WaitFor(sampleDb);

builder.AddProject<Projects.Monivus_UI>("ui")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WithReference(cache)
    .WaitFor(api)
    .WaitFor(cache);

builder.Build().Run();
