namespace osuRequestor.Features.Requests.Models;

public record PostBaseRequest
{
    public int? DestinationId { get; set; }
    public int? BeatmapId { get; set; }

    public void Deconstruct(out int? destinationId, out int? beatmapId)
    {
        destinationId = this.DestinationId;
        beatmapId     = this.BeatmapId;
    }
}
