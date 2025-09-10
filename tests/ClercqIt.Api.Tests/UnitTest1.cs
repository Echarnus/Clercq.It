using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace ClercqIt.Api.Tests;

public class WeatherForecastTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WeatherForecastTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsValidJson()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.NotNull(content);
        Assert.NotEmpty(content);
        
        // Verify it's valid JSON
        var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsValidWeatherData()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();
        var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Assert
        Assert.NotNull(forecasts);
        foreach (var forecast in forecasts)
        {
            Assert.NotNull(forecast.Summary);
            Assert.NotEmpty(forecast.Summary);
            Assert.InRange(forecast.TemperatureC, -20, 55);
            Assert.True(forecast.Date > DateOnly.MinValue);
        }
    }

    public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}

public class ProjectTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProjectTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProjects_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsValidJson()
    {
        // Act
        var response = await _client.GetAsync("/projects");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.NotNull(content);
        Assert.NotEmpty(content);
        
        // Verify it's valid JSON
        var projects = JsonSerializer.Deserialize<Project[]>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(projects);
        Assert.Equal(5, projects.Length);
    }

    [Fact]
    public async Task GetProjects_ReturnsValidProjectData()
    {
        // Act
        var response = await _client.GetAsync("/projects");
        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<Project[]>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Assert
        Assert.NotNull(projects);
        foreach (var project in projects)
        {
            Assert.True(project.Id > 0);
            Assert.NotNull(project.Name);
            Assert.NotEmpty(project.Name);
            Assert.NotNull(project.Description);
            Assert.NotEmpty(project.Description);
            Assert.NotNull(project.Status);
            Assert.NotEmpty(project.Status);
            Assert.True(project.CreatedDate > DateTime.MinValue);
        }
    }

    [Fact]
    public async Task GetProjects_ReturnsExpectedProjectNames()
    {
        // Act
        var response = await _client.GetAsync("/projects");
        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<Project[]>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Assert
        Assert.NotNull(projects);
        var projectNames = projects.Select(p => p.Name).ToArray();
        Assert.Contains("Project Alpha", projectNames);
        Assert.Contains("Project Beta", projectNames);
        Assert.Contains("Project Gamma", projectNames);
        Assert.Contains("Project Delta", projectNames);
        Assert.Contains("Project Epsilon", projectNames);
    }

    public record Project(int Id, string Name, string Description, string Status, DateTime CreatedDate);
}