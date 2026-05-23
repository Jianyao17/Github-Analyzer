using System.IdentityModel.Tokens.Jwt;
using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GithubAnalyzer.WebApi.Services.Auth;

namespace GithubAnalyzer.WebApi.Tests.Services;

public sealed class JwtIdentityServiceTests
{
    private static JwtConfig BuildConfig(int expiryMinutes = 60) => new()
    {
        Issuer              = "test-issuer",
        Audience            = "test-audience",
        SecretKey           = "super-secret-key-that-is-long-enough-for-hmac256!",
        ExpirationInMinutes = expiryMinutes
    };

    private static ApplicationUser BuildUser() => new()
    {
        Id       = Guid.NewGuid(),
        UserName = "tester",
        Email    = "tester@example.com"
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Happy path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_ReturnsNonEmptyJwtString()
    {
        var svc   = new JwtIdentityService(BuildConfig());
        var token = svc.CreateToken(BuildUser());

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void CreateToken_TokenIsValidJwt()
    {
        var cfg   = BuildConfig();
        var svc   = new JwtIdentityService(cfg);
        var token = svc.CreateToken(BuildUser());

        var handler    = new JwtSecurityTokenHandler();
        var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.SecretKey));
        var validation = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidIssuer      = cfg.Issuer,
            ValidateAudience = true,
            ValidAudience    = cfg.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = key,
            ClockSkew        = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(token, validation, out _);
        Assert.NotNull(principal);
    }

    [Fact]
    public void CreateToken_ContainsSubjectClaim()
    {
        var user  = BuildUser();
        var svc   = new JwtIdentityService(BuildConfig());
        var token = svc.CreateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

        Assert.NotNull(sub);
        Assert.Equal(user.Id.ToString(), sub!.Value);
    }

    [Fact]
    public void CreateToken_ContainsEmailClaim()
    {
        var user  = BuildUser();
        var svc   = new JwtIdentityService(BuildConfig());
        var token = svc.CreateToken(user);

        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);

        Assert.NotNull(email);
        Assert.Equal(user.Email, email!.Value);
    }

    [Fact]
    public void CreateToken_ContainsPreferredUsernameClaim()
    {
        var user  = BuildUser();
        var svc   = new JwtIdentityService(BuildConfig());
        var token = svc.CreateToken(user);

        var jwt      = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var username = jwt.Claims
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.PreferredUsername);

        Assert.NotNull(username);
        Assert.Equal(user.UserName, username!.Value);
    }

    [Fact]
    public void CreateToken_ExpiryMatchesConfiguration()
    {
        const int expiryMinutes = 30;
        var svc    = new JwtIdentityService(BuildConfig(expiryMinutes));
        var before = DateTime.UtcNow;
        var token  = svc.CreateToken(BuildUser());
        var after  = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // JWT truncates expiry to whole seconds, so allow -1s tolerance on lower bound
        Assert.True(jwt.ValidTo >= before.AddMinutes(expiryMinutes).AddSeconds(-1),
            $"ValidTo {jwt.ValidTo:O} should be on or after {before.AddMinutes(expiryMinutes).AddSeconds(-1):O}");
        Assert.True(jwt.ValidTo <= after.AddMinutes(expiryMinutes).AddSeconds(5),
            $"ValidTo {jwt.ValidTo:O} should be on or before {after.AddMinutes(expiryMinutes).AddSeconds(5):O}");
    }

    [Fact]
    public void CreateToken_UsesHmacSha256Algorithm()
    {
        var svc   = new JwtIdentityService(BuildConfig());
        var token = svc.CreateToken(BuildUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Distinct tokens per invocation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_DifferentUsersProduceDifferentTokens()
    {
        var svc    = new JwtIdentityService(BuildConfig());
        var token1 = svc.CreateToken(BuildUser());
        var token2 = svc.CreateToken(BuildUser());   // different Guid

        Assert.NotEqual(token1, token2);
    }
}
