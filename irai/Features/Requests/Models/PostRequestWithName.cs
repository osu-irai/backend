namespace osuRequestor.Features.Requests.Models;

public record PostRequestWithName
{
    public string? DestinationName { get; set; }
    public int? BeatmapId { get; set; }

    public void Deconstruct(out int? beatmapId, out string? destinationName)
    {
        beatmapId       = this.BeatmapId;
        destinationName = this.DestinationName;
    }
}
