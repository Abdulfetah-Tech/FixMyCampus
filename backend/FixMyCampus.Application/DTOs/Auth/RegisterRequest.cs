using System.ComponentModel.DataAnnotations;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Auth;
public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string UniversityId { get; set; } = string.Empty;
        [EnumDataType(typeof(RegistrationRole))]
    public RegistrationRole RequestedRole { get; set; } = RegistrationRole.Reporter;
}