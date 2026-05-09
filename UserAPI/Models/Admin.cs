using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace UserAPI.Models;

public class Admin
{
    [Required]
    [BsonId]
    public int AdminId { get; set; }
    [Required]
    public string Username { get; set; }
    public string Password { get; set; }
}