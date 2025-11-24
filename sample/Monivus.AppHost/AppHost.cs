
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");


var postgres = builder
    .AddPostgres("postgres")
    .WithHostPort(1111)
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDb = postgres.AddDatabase("postgresDb");

var mongo = builder.AddMongoDB("mongo");
var mongodb = mongo.AddDatabase("mongodb");

#region Other Test Integrations
//var oracle = builder.AddOracle("oracle");
//var oracledb = oracle.AddDatabase("oracledb");

//var mysql = builder.AddMySql("mysql");
//var mySqlDb = mysql.AddDatabase("mySqlDb");

//var sqlServer = builder.AddSqlServer("sqlserver");
//var sampleDb = sqlServer.AddDatabase("sampleDb");
#endregion

var api = builder.AddProject<Projects.Monivus_Api>("api")
    .WithReference(mongodb)
    .WaitFor(mongodb);

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
