namespace osuRequestor.DTO.Requests;

public record PostBaseRequest
{
    public int? DestinationId { get; set; }
    public int? BeatmapId { get; set; }

    public void Deconstruct(out int? destinationId, out int? beatmapId)
    {
        destinationId = DestinationId;
        beatmapId = BeatmapId;
    }
}