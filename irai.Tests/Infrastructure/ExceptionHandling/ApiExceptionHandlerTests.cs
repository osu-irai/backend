using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using osuRequestor.Common.Exceptions;
using osuRequestor.Infrastructure.ExceptionHandling;

namespace osuRequestor.Tests.Infrastructure.ExceptionHandling;

[TestFixture]
public class ApiExceptionHandlerTests
{
    [Test]
    public async Task TryHandleAsync_IgnoresNonApiExceptions()
    {
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
                          context,
                          new InvalidOperationException("unexpected"),
                          CancellationToken.None);

        Assert.That(handled, Is.False);
    }

    [Test]
    public async Task TryHandleAsync_MapsNotFoundExceptionTo404()
    {
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
                          context,
                          new NotFoundException("Beatmap 1"),
                          CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.That(handled,                     Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(body,                        Is.EqualTo("Beatmap 1"));
    }
}
