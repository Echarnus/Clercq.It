var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/projects", () =>
{
    var projects = new[]
    {
        new Project(1, "Project Alpha", "A cutting-edge web application for modern businesses", "Active", DateTime.Now.AddDays(-30)),
        new Project(2, "Project Beta", "Mobile app development for iOS and Android", "In Progress", DateTime.Now.AddDays(-15)),
        new Project(3, "Project Gamma", "Data analytics and visualization platform", "Completed", DateTime.Now.AddDays(-60)),
        new Project(4, "Project Delta", "E-commerce solution with advanced features", "Planning", DateTime.Now.AddDays(-5)),
        new Project(5, "Project Epsilon", "AI-powered customer service chatbot", "Active", DateTime.Now.AddDays(-20))
    };
    return projects;
})
.WithName("GetProjects");

app.Run();

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record Project(int Id, string Name, string Description, string Status, DateTime CreatedDate);

// Make the Program class public for testing
public partial class Program { }
