using NUnit.Framework;
using osuRequestor.Common.Models;
using osuRequestor.Features.Requests.Mappings;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Requests;

[TestFixture]
public class MapperTests
{
    [Test]
    public void BeatmapModel_IntoDTO_MapsBeatmapAndSetFields()
    {
        var model = new BeatmapModel
        {
            Id         = 100,
            Version    = "Insane",
            StarRating = 5.25,
            BeatmapSet = new BeatmapSetModel
            {
                Id     = 50,
                Artist = "Artist",
                Title  = "Title"
            }
        };

        var dto = model.IntoDTO();

        Assert.Multiple(() =>
        {
            Assert.That(dto.BeatmapId,    Is.EqualTo(100));
            Assert.That(dto.BeatmapsetId, Is.EqualTo(50));
            Assert.That(dto.Artist,       Is.EqualTo("Artist"));
            Assert.That(dto.Title,        Is.EqualTo("Title"));
            Assert.That(dto.Difficulty,   Is.EqualTo("Insane"));
            Assert.That(dto.Stars,        Is.EqualTo(5.25));
        });
    }

    [Test]
    public void ReceivedRequestResponse_ToRequest_SetsTargetAndRequest()
    {
        var response = new ReceivedRequestResponse
        {
            Id = 10,
            Beatmap = new BeatmapDTO
            {
                BeatmapId    = 100,
                BeatmapsetId = 50,
                Artist       = "Artist",
                Title        = "Title",
                Difficulty   = "Insane",
                Stars        = 5.25
            },
            Source = RequestSource.Website
        };

        var request = response.ToRequest("alice");

        Assert.Multiple(() =>
        {
            Assert.That(request.Target,  Is.EqualTo("alice"));
            Assert.That(request.Request, Is.SameAs(response));
        });
    }
}
