using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GithubAnalyzer.WebApi.Extensions;

public static class JwtAuthBuilderExtensions
{
    public static IHostApplicationBuilder AddJwtAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>()
            ?? throw new InvalidOperationException("JWT settings are missing.");
        
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey));
        var googleConfig = builder.Configuration
            .GetSection("Authentication:Google")
            .Get<GoogleAuthConfig>() ?? new GoogleAuthConfig();

        
        services.AddIdentityCore<ApplicationUser>(builder.LoadIdentityConfig())
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton(jwtConfig);
        services.AddScoped<JwtIdentityService>();

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = 
                    new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = signingKey,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["token"];
                        var path = context.HttpContext.Request.Path;

                        // TODO: This is a bit of a hack to allow JWT authentication for the SSE endpoint,
                        //       which cannot send the token in the Authorization header.
                        // This allows us to get the token from the query string for the SSE connection
                        if (!string.IsNullOrEmpty(accessToken) && 
                            path.StartsWithSegments("/api/v1") && 
                            path.Value!.EndsWith("/queue/event"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "github-analyzer.external";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

        if (googleConfig.IsEnabled)
        {
            authenticationBuilder.AddGoogle(
                GoogleDefaults.AuthenticationScheme,
                options =>
                {
                    options.ClientId = googleConfig.ClientId;
                    options.ClientSecret = googleConfig.ClientSecret;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                    options.CallbackPath = googleConfig.CallbackPath;

                    // Map additional claims from Google's user info response
                    options.ClaimActions.MapJsonKey("urn:google:picture", "picture");
                    options.ClaimActions.MapJsonKey("urn:google:email_verified", "email_verified");
                });
        }

        services.AddAuthorization();

        return builder;
    }
}
