using System.ComponentModel.DataAnnotations;

namespace EfCore.Application.Dtos.Auth;

public sealed class RegisterRequest
{
  [Required]
  [StringLength(100, MinimumLength = 10)]
  [EmailAddress]
  public string Email {get; init;} = string.Empty;

  [Required]
  [StringLength(20, MinimumLength = 8)]
  public string Password {get; init; } = string.Empty;

  [Required]
  [StringLength(120, MinimumLength = 2)]
  public string DisplayName {get; init;} = string.Empty;
}

public enum RegisterStatus
{
  Created, DuplicateEmail, ValidationError
}
public sealed record RegisterResult(
  RegisterStatus Status,
  AuthUserResponse? User,
  string[] Error
);