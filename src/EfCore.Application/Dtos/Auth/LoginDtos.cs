using System.ComponentModel.DataAnnotations;

namespace EfCore.Application.Dtos.Auth;

public sealed class LoginRequest
{
  [EmailAddress, Required]
  public string Email {get; init;} = string.Empty;

  [Required]
  public string Password {get; init;} = string.Empty;
}

public sealed record LoginResponse(
  string Message,
  AuthUserResponse? User,
  string AccessToken,
  string TokenType,
  DateTimeOffset ExpiredAtUtc
);

public sealed record LoginResults(
  bool Succeeded,
  AuthUserResponse? User
);
