using System.Diagnostics.CodeAnalysis;

namespace osuRequestor.Common.Models;

public class UserDTO
{
    public UserDTO() { }

    [SetsRequiredMembers]
    public UserDTO(int id, string username, string avatarUrl)
    {
        this.Id        = id;
        this.Username  = username;
        this.AvatarUrl = avatarUrl;
    }

    public required int Id { get; set; }

    public required string Username { get; set; }

    public required string AvatarUrl { get; set; }
}
