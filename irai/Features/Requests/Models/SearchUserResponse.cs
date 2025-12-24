using osuRequestor.Common.Models;

namespace osuRequestor.Features.Requests.Models;

public record SearchUserResponse
{
    public required List<UserDTO> Players { get; set; }
    public required int Count { get; set; }
}
