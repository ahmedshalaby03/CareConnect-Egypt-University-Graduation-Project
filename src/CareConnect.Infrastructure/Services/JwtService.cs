using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CareConnect.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;

        if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. Set it in user secrets, " +
                "appsettings.Development.json or an environment variable.");
        }
    }

    public AccessToken CreateAccessToken(ApplicationUser user, string role)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id),
            new(AppClaimTypes.FullName, user.FullName),
            new(AppClaimTypes.Email, user.Email ?? string.Empty),
            new(AppClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return new AccessToken(handler.CreateToken(descriptor), expires);
    }

    public RefreshTokenPair CreateRefreshToken()
    {
        // 64 bytes of CSPRNG output; the client gets this once and we keep only its hash.
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new RefreshTokenPair(
            raw,
            HashRefreshToken(raw),
            DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
    }

    public string HashRefreshToken(string rawToken)
    {
        // Plain SHA-256 rather than a slow KDF: the token is 512 bits of random data, so
        // there is no low-entropy secret for an attacker to brute force.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
