using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Models;

[PrimaryKey(nameof(Id))]
public class UserModel
{
    public int Id { get; set; }
    [DataType(DataType.Text), MaxLength(20)]
    public string Username { get; set; }

    public UserModel(int id, string username)
    {
        Id = id;
        Username = username;
    }

}