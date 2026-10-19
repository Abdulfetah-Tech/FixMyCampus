using System.ComponentModel.DataAnnotations;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Auth;

public class ApproveUserRequest
{
    // Optional: when null, the admin accepts the role the user asked for.
    [EnumDataType(typeof(RegistrationRole))]
    public RegistrationRole? Role { get; set; }
}