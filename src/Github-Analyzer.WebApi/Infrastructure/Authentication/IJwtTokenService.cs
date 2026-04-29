using GithubAnalyzer.WebApi.Database;

namespace GithubAnalyzer.WebApi.Infrastructure.Authentication;

public interface IJwtTokenService
{
    AuthResponse CreateToken(ApplicationUser user);
}
