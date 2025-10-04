var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("ClercqItDb");

// Add migration project to run on startup
var migrations = builder.AddProject<Projects.Clercq_It_Infrastructure_EF_Migrations>("migrations")
    .WithReference(database)
    .WaitFor(database);

// Add the API project
var api = builder.AddProject<Projects.ClercqIt_Api>("clercqit-api")
    .WithReference(database)
    .WaitForCompletion(migrations);

// Add the Next.js frontend
var web = builder.AddNodeApp("clercqit-web", "../ClercqIt.Web")
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
