namespace osuRequestor.DTO.Requests;

public record PostNamedRequestRequest
{
    public string? DestinationName { get; set; }
    public int? BeatmapId { get; set; }

    public void Deconstruct(out string? destinationName, out int? beatmapId)
    {
        destinationName = DestinationName;
        beatmapId = BeatmapId;
    }
 
};