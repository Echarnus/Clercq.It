using Clercq.It.Application.Common.Behaviors;
using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Name) : IRequest<string>;

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);

        // Act
        var result = await behavior.Handle(new SampleRequest("ok"), _ => Task.FromResult("handled"), CancellationToken.None);

        // Assert
        result.Should().Be("handled");
    }

    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ShouldThrowWithoutCallingNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);
        var nextCalled = false;

        // Act
        var act = () => behavior.Handle(new SampleRequest(""), _ =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoValidatorsExist_ShouldCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<SampleRequest, string>([]);

        // Act
        var result = await behavior.Handle(new SampleRequest(""), _ => Task.FromResult("handled"), CancellationToken.None);

        // Assert
        result.Should().Be("handled");
    }
}
