var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb()
    .AddDatabase("swissbowling");

builder.AddProject<Projects.SwissBowling_Server>("server")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.Build().Run();
