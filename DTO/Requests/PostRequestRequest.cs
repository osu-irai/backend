namespace osuRequestor.DTO.Requests;

public record PostRequestRequest
{
    public int? SourceId { get; set; }
    public int DestinationId { get; set; }
    public int BeatmapId { get; set; }

    public void Deconstruct(out int? sourceId, out int destinationId, out int beatmapId)
    {
        sourceId = SourceId;
        destinationId = DestinationId;
        beatmapId = BeatmapId;
    }
}