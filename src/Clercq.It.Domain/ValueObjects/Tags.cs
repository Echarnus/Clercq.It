namespace Clercq.It.Domain.ValueObjects;

public record Tags(string[] Values)
{
    public static implicit operator Tags(string[] values) => new(values);
    public static implicit operator string[](Tags tags) => tags.Values;
    
    public override string ToString() => string.Join(", ", Values);
}