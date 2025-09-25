namespace ClercqIt.Api.Data;

public class HealthCheck
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Details { get; set; }
}