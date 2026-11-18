using irai.Common.Models;

namespace irai.Features.Search.Models;

public record SearchUserResponse
{
    public required List<UserDTO> Players { get; set; }
    public required int Count { get; set; }
}
