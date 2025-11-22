var builder = DistributedApplication.CreateBuilder(args);

var cache = builder
    .AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);

var sqlServer = builder
    .AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);

var sampleDb = sqlServer.AddDatabase("sampleDb");

var api = builder.AddProject<Projects.Monivus_Api>("api")
    .WithReference(sampleDb)
    .WaitFor(sampleDb);

var postgres = builder
    .AddPostgres("postgres")
    .WithHostPort(1111)
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDb = postgres.AddDatabase("postgresDb");

//var oracle = builder.AddOracle("oracle")
//                    .WithLifetime(ContainerLifetime.Persistent);

//var oracledb = oracle.AddDatabase("oracledb");

//var mysql = builder.AddMySql("mysql")
//    .WithLifetime(ContainerLifetime.Persistent);

//var mySqlDb = mysql.AddDatabase("mySqlDb");

var api2 = builder.AddProject<Projects.Monivus_Api2>("api2")
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

builder.AddProject<Projects.Monivus_UI>("ui")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WithReference(cache)
    .WithReference(api2)
    .WaitFor(api)
    .WaitFor(cache)
    .WaitFor(api2);

builder.Build().Run();
