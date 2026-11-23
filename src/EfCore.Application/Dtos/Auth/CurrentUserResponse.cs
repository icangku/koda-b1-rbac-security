namespace EfCore.Application.Dtos.Auth;

public sealed record CurrentUserReponse(
    Guid Id, 
    string Email,
    IReadOnlyList<string> Roles
);
