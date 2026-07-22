using CareConnect.Domain.Entities;

namespace CareConnect.Application.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>A refresh token in both forms: the raw value goes to the client, the hash to the database.</summary>
public record RefreshTokenPair(string RawToken, string TokenHash, DateTime ExpiresAt);

public interface IJwtService
{
    AccessToken CreateAccessToken(ApplicationUser user, string role);

    RefreshTokenPair CreateRefreshToken();

    /// <summary>Hashes a raw refresh token so an incoming value can be matched against stored hashes.</summary>
    string HashRefreshToken(string rawToken);
}
