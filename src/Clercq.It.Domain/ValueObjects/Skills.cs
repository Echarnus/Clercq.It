namespace Clercq.It.Domain.ValueObjects;

public record Skills(string[] Values)
{
    public static implicit operator Skills(string[] values) => new(values);
    public static implicit operator string[](Skills skills) => skills.Values;
    
    public override string ToString() => string.Join(", ", Values);
}