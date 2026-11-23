using System;

namespace EfCore.Infrastructure.Identity;

public sealed class RefreshToken
{
  public Guid Id {get; set;}
  public Guid UserId {get; set;}
  public AppUser User {get; set;} = null!;
  public Guid FamilyId {get; set;} //1
  public string TokenHash {get; set;} = string.Empty;
  public DateTimeOffset CreatedAtUtc {get; set;}
  public DateTimeOffset ExpiredAtUtc {get; set;}
  public DateTimeOffset? UsedAtUtc {get; set;}
  public DateTimeOffset? RevokedAtUtc {get; set;}
}
