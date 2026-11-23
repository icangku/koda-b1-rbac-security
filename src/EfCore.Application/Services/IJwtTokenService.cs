using EfCore.Application.Dtos.Auth;

namespace EfCore.Application.Services;

public interface IJwtTokenService
{
  IssuedAccessToken IssueAccessToken(AuthUserResponse user);
}