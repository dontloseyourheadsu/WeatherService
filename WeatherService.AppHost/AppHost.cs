var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongo");
var db = mongo.AddDatabase("WeatherDb");

var api = builder.AddProject<Projects.WeatherService_Api>("weather-api")
    .WithReference(db);

builder.AddProject<Projects.WeatherService_WebApp>("weather-webapp")
    .WithReference(api);

builder.AddProject<Projects.WeatherService_Seeding>("weather-seeder")
    .WithReference(db);

builder.Build().Run();
