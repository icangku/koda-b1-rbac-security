namespace EfCore.Application.Dtos.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles
);
