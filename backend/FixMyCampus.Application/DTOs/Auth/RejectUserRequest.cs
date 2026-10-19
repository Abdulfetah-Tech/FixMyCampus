using System.ComponentModel.DataAnnotations;

namespace FixMyCampus.Application.DTOs.Auth;

public class RejectUserRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}