var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("ClercqItDb");

// Add the API project
var api = builder.AddProject<Projects.ClercqIt_Api>("clercqit-api")
    .WithReference(database);

// Add the Next.js frontend
var web = builder.AddNodeApp("clercqit-web", "../ClercqIt.Web")
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
