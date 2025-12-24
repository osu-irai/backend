using osuRequestor.Common.Models;

namespace osuRequestor.Features.Users.Models;

public record SelfUserResponse
{
    public required UserDTO User { get; set; }

    public required int RequestCount { get; set; }
}
