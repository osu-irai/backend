using ChiffadicErrors;
using irai.Common.Exceptions;
using irai.Features.Requests.Models;
using irai.Features.Requests.Persistence;
using irai.Features.Requests.Services;
using irai.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace irai.Tests.Infrastructure.Messaging;

[TestFixture]
public class MessageSubscriberServiceTests
{
    private static (MessageSubscriberService Sut, IRequestService Requests) CreateHarness(
        Result<int, RequestServiceError>? result = null,
        Exception? failure = null)
    {
        var requests = Substitute.For<IRequestService>();
        var call = requests.CreateRequest(Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<int>(),
                                          Arg.Any<RequestSource>());
        if (failure is not null)
        {
            call.ThrowsAsync(failure);
        }
        else
        {
            call.Returns(result ?? new OkVariant<int>(1));
        }

        return (new MessageSubscriberService(NullLogger<MessageSubscriberService>.Instance, requests), requests);
    }

    private static ConsumeContext<PostBaseRequest> CreateContext()
    {
        var context = Substitute.For<ConsumeContext<PostBaseRequest>>();
        context.Message.Returns(new PostBaseRequest { DestinationId = 2, BeatmapId = 100 });
        return context;
    }

    [Test]
    public async Task Consume_WhenRequestSucceeds_ReturnsWithoutThrowing()
    {
        var (sut, _) = CreateHarness();

        await Assert.DoesNotThrowAsync(() => sut.Consume(CreateContext()));
    }

    [Test]
    public async Task Consume_WhenServiceThrowsNotFound_DoesNotThrow()
    {
        var (sut, _) = CreateHarness(failure: new NotFoundException("User 2"));

        await Assert.DoesNotThrowAsync(() => sut.Consume(CreateContext()));
    }

    [Test]
    public async Task Consume_WhenServiceThrowsUnauthorized_DoesNotThrow()
    {
        var (sut, _) = CreateHarness(failure: new UnauthorizedException());

        await Assert.DoesNotThrowAsync(() => sut.Consume(CreateContext()));
    }

    [Test]
    public async Task Consume_WhenServiceThrowsServerError_DoesNotThrow()
    {
        var (sut, _) = CreateHarness(failure: new ServerErrorException("upstream"));

        await Assert.DoesNotThrowAsync(() => sut.Consume(CreateContext()));
    }
}
