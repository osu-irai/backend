using osuRequestor.DTO.General;

namespace osuRequestor.DTO.Responses;

public class SelfUserResponse
{
    public required UserDTO User { get; set; }

    public required int RequestCount { get; set; }
}