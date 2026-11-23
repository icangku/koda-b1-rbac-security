namespace EfCore.Application;

public sealed record IssuedAccessToken(
  string AccessToken,
  DateTimeOffset ExpiredAtUtc
);