using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Models;

public class UserModel
{
    public int Id { get; set; }
    [DataType(DataType.Text), MaxLength(20)]
    public required string Username { get; set; }
    }