using Amazon.S3;
using Clercq.It.ContentImport;
using Clercq.It.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Imports repo content (content/**/*.md) into the database. Runs in deploy.yml
// after the migrations. Usage: dotnet run --project src/Clercq.It.ContentImport -- <content-folder>
// Config (environment): ConnectionStrings__DefaultConnection, ObjectStorage__*.
var contentRoot = args.Length > 0 ? args[0] : "content";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ContentImporter>();
builder.Services.AddScoped<IContentImageStore>(sp =>
    sp.GetService<IAmazonS3>() is null
        ? new UnconfiguredImageStore()
        : ActivatorUtilities.CreateInstance<S3ContentImageStore>(sp));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

try
{
    var result = await scope.ServiceProvider.GetRequiredService<ContentImporter>().ImportAsync(contentRoot);
    Console.WriteLine($"Content import done: {result.Created} created, {result.Updated} updated, {result.Unchanged} unchanged");
    return 0;
}
catch (ContentException ex)
{
    Console.Error.WriteLine($"Content error: {ex.Message}");
    return 1;
}

// Lets content without local images import when ObjectStorage is not configured.
internal sealed class UnconfiguredImageStore : IContentImageStore
{
    public Task<string> UploadAsync(string localPath, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"ObjectStorage is not configured; it is needed to upload {localPath}.");
}
