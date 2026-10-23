using Clercq.It.Api;
using Clercq.It.Application.Features.MyZen;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ClercqIt.Api.Tests.MyZen;

public class MyZenExceptionHandlerTests
{
    public static TheoryData<Exception, int> Handled => new()
    {
        { new MyZenNotConnectedException(), StatusCodes.Status409Conflict },
        { new MyZenUnavailableException("down"), StatusCodes.Status502BadGateway },
    };

    [Theory(DisplayName = "MyZen errors become 409 or 502 problem responses")]
    [MemberData(nameof(Handled))]
    public async Task MyZenError_Handled(Exception exception, int status)
    {
        // Arrange
        var context = Context();

        // Act
        var handled = await new MyZenExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(status);
    }

    [Fact(DisplayName = "Other errors are left to the next handler")]
    public async Task OtherError_NotHandled()
    {
        (await new MyZenExceptionHandler().TryHandleAsync(Context(), new InvalidOperationException(), CancellationToken.None))
            .Should().BeFalse();
    }

    private static DefaultHttpContext Context() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider(),
        Response = { Body = new MemoryStream() },
    };
}
