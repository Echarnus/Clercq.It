using System.Text.Json;
using Clercq.It.Api;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ClercqIt.Api.Tests;

public class ValidationExceptionHandlerTests
{
    private static DefaultHttpContext NewContext() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        Response = { Body = new MemoryStream() },
    };

    [Fact]
    public async Task TryHandleAsync_OtherException_ReturnsFalseAndWritesNothing()
    {
        // Arrange
        var handler = new ValidationExceptionHandler();
        var context = NewContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        // Assert
        handled.Should().BeFalse();
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_Writes400WithErrorsGroupedPerField()
    {
        // Arrange
        var handler = new ValidationExceptionHandler();
        var context = NewContext();
        var exception = new ValidationException(
        [
            new ValidationFailure("Title", "Title is required"),
            new ValidationFailure("Title", "Title is required"),
            new ValidationFailure("Title", "Title is too long"),
            new ValidationFailure("Issuer", "Issuer is required"),
        ]);

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        var errors = body.RootElement.GetProperty("errors");
        errors.GetProperty("Title").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("Title is required", "Title is too long");
        errors.GetProperty("Issuer").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("Issuer is required");
    }
}
