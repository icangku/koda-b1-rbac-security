namespace EfCore.Application.Dtos.Auth;

public sealed record class RefreshTokenSessionResult
(
  AuthUserResponse User,
  IssuedAccessToken AccessToken,
  string RawRefreshToken,
  DateTimeOffset RefreshExpiredAtUtc
);
