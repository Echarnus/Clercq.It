var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("ClercqItDb");

// Add Keycloak for local authentication
// Uses port 8080 for stable browser cookie handling
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithRealmImport("./KeycloakRealms");

// MinIO for local S3-compatible object storage is run separately for fixed port binding:
// docker run -d --name minio-dev -p 9100:9000 -p 9101:9001 -e "MINIO_ROOT_USER=minioadmin" -e "MINIO_ROOT_PASSWORD=minioadmin" -v minio-data:/data minio/minio server /data --console-address ":9001"
// Then create bucket: docker exec minio-dev mc alias set myminio http://localhost:9000 minioadmin minioadmin && docker exec minio-dev mc mb --ignore-existing myminio/clercq-it-dev && docker exec minio-dev mc anonymous set download myminio/clercq-it-dev

// Add migration project to run on startup
var migrations = builder.AddProject<Projects.Clercq_It_Infrastructure_EF_Migrations>("migrations")
    .WithReference(database)
    .WaitFor(database);

// Add the API project with fixed port 5001 for stable frontend access (port 5000 blocked by macOS AirPlay)
var api = builder.AddProject<Projects.ClercqIt_Api>("clercqit-api")
    .WithReference(database)
    .WithReference(keycloak)
    .WaitForCompletion(migrations)
    .WaitFor(keycloak)
    .WithHttpEndpoint(port: 5001, name: "api-http")
    .WithExternalHttpEndpoints();

// Add the Next.js frontend with fixed port 3000 for Keycloak redirect URIs
// Path is relative to AppHost directory
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "ClercqIt.Web");
var web = builder.AddNpmApp("clercqit-web", frontendPath, "dev")
    .WithReference(api)
    .WithReference(keycloak)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithEnvironment("NEXT_PUBLIC_API_URL", "http://localhost:5001")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
