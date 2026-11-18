using irai.Common.Models;

namespace irai.Features.Users.Models;

public record SelfUserResponse
{
    public required UserDTO User { get; set; }

    public required int RequestCount { get; set; }
}
