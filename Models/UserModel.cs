using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Apis.OsuApi.Models;

namespace osuRequestor.Models;

public class UserModel
{
    [Key]
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string CountryCode { get; set; }

    public string AvatarUrl { get; set; } = null!;

    public TokenModel Token { get; set; } = null!;

    public User IntoApiModel()
    {
        return new User
        {
            Id = this.Id,
            Username = this.Username,
            CountryCode = this.CountryCode,
            AvatarUrl = this.AvatarUrl,
        };
    }
}