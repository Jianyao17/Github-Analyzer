using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace GithubAnalyzer.WebApi.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_ReturnsJwtPayloadForUser()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "Github-Analyzer",
            Audience = "Github-Analyzer.WebApp",
            Key = "ThisIsADevelopmentKeyWithEnoughLength1234567890",
            ExpiryMinutes = 60
        });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            UserName = "tester@example.com",
            DisplayName = "Tester"
        };

        var service = new JwtTokenService(options);
        var result = service.CreateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.DisplayName, result.User.DisplayName);
    }
}
