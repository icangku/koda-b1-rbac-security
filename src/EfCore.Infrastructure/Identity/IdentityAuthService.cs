using EfCore.Application.Dtos.Auth;
using EfCore.Application.Security;
using EfCore.Application.Services;
using EfCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Infrastructure.Identity;

public sealed class IdentityAuthService(UserManager<AppUser> user, AppDbContext db) : IAuthService
{
  private readonly UserManager<AppUser> _user = user;
  private readonly AppDbContext _db = db;

  public async Task<LoginResults> LoginAsync(LoginRequest request, CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();

    var email = request.Email.Trim();
    var user = await _user.FindByEmailAsync(email);

    if (user is null) return new LoginResults(false, null);

    var passwordValid = await _user.CheckPasswordAsync(user, request.Password);
    if (!passwordValid) return new LoginResults(false, null);

    var roles = await _user.GetRolesAsync(user);
    var response = new AuthUserResponse(user.Id, user.Email!, user.DisplayName, roles.ToArray());
    return new LoginResults(true, response);
  }

  public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();
    var email = request.Email.Trim();
    var existingUser = await _user.FindByEmailAsync(email);
    if (existingUser is not null)
    {
      return new RegisterResult(RegisterStatus.DuplicateEmail, null, ["Email sudah terdaftar"]);
    }

    var user = new AppUser
    {
      Id = Guid.NewGuid(),
      UserName = email,
      Email = email,
      DisplayName = request.DisplayName.Trim(),
      CreatedAtUtc = DateTimeOffset.UtcNow
    };

    await using var transaction = await _db.Database.BeginTransactionAsync(ct);

    try
    {
      var createResult = await _user.CreateAsync(user, request.Password);

      if (!createResult.Succeeded)
      {
        await transaction.RollbackAsync(ct);
        var errors = createResult.Errors.ToArray();
        var duplicate = errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");

        if (duplicate)
        {
          return new RegisterResult(RegisterStatus.DuplicateEmail, null, ["Email sudah terdaftar."]);
        }

        return new RegisterResult(
          RegisterStatus.ValidationError,
          null,
          errors.Select(error => error.Description).ToArray()
        );
      }

      var roleResult = await _user.AddToRoleAsync(user, AppRoles.Customer);

      if (!roleResult.Succeeded)
      {
        await transaction.RollbackAsync(ct);
        var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Gagal memberikan role CUSTOMER: {errors}");
      }

      await transaction.CommitAsync(ct);
    }
    catch (DbUpdateException e)
      when (e.InnerException is SqlException sqlError && sqlError.Number is 2601 or 2627)
    {
      return new RegisterResult(RegisterStatus.DuplicateEmail, null, ["Email sudah terdaftar"]);
    }

    var roles = await _user.GetRolesAsync(user);
    var response = new AuthUserResponse(user.Id, user.Email!, user.DisplayName, roles.ToArray());

    return new RegisterResult(RegisterStatus.Created, response, []);
  }
}
