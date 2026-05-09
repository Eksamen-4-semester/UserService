using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace UserAPI.Models;

public class Member
{
    [Required]
    [BsonId]
    public int MemberId { get; set; }
    [Required]
    public DateTime SignUpDate { get; set; }
    [Required]
    public string FullName { get; set; }
    [Required]
    public DateTime DOB { get; set; }
    [Required]
    public string Username { get; set; }
    public string Password { get; set; }
    [Required]
    public bool Active { get; set; }
    public DateTime? InactiveDate { get; set; }
}