using System.Diagnostics.CodeAnalysis;

namespace osuRequestor.DTO.General;

public class UserDTO
{
    public UserDTO()
    {
    }

    [SetsRequiredMembers]
    public UserDTO(int id, string username, string avatarUrl)
    {
        Id = id;
        Username = username;
        AvatarUrl = avatarUrl;
    }

    public required int Id { get; set; }
    
    public required string Username { get; set; }
    
    public required string AvatarUrl { get; set; }

}