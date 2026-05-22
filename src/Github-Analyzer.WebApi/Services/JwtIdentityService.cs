using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Config;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GithubAnalyzer.WebApi.Services;

public sealed class JwtIdentityService(JwtConfig config)
{
    public string CreateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.PreferredUsername, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(config.ExpirationInMinutes);

        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims, expires: expiration,
            signingCredentials: credentials,
            notBefore: DateTime.UtcNow);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
