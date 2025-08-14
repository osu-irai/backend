using osuRequestor.DTO.General;

namespace osuRequestor.DTO.Responses;

public record SearchUserResponse
{
   public required List<UserDTO> Players { get; set; }
   public required int Count { get; set; }
}