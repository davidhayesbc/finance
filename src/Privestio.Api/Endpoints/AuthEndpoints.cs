using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Infrastructure.Data;
using Privestio.Infrastructure.Identity;

namespace Privestio.Api.Endpoints;

public static class AuthEndpoints
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group
            .MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group
            .MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Login with email and password")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group
            .MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Exchange a refresh token for a new access/refresh token pair")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group
            .MapPost("/revoke", RevokeAsync)
            .WithName("RevokeToken")
            .WithSummary("Revoke a refresh token (logout)");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        PrivestioDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        // Create the domain user first
        var domainUser = new User(request.Email, request.DisplayName);

        // Create the Identity user
        var identityUser = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            DomainUserId = domainUser.Id,
            EmailConfirmed = true, // TODO: Implement email verification when SMTP is available
        };

        var createResult = await userManager.CreateAsync(identityUser, request.Password);
        if (!createResult.Succeeded)
        {
            return Results.ValidationProblem(
                createResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
            );
        }

        // Link identity user ID back to domain user
        domainUser.IdentityUserId = identityUser.Id;
        dbContext.DomainUsers.Add(domainUser);

        var accessToken = GenerateJwtToken(identityUser, domainUser, configuration);
        var refreshToken = new RefreshToken(domainUser.Id, RefreshTokenLifetime);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/users/{domainUser.Id}",
            BuildAuthResponse(accessToken, refreshToken.Token, request.Email, request.DisplayName, domainUser.Id)
        );
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        PrivestioDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email);
        if (identityUser is null)
            return Results.Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(
            identityUser,
            request.Password,
            lockoutOnFailure: true
        );

        if (result.IsLockedOut)
            return Results.Problem(
                detail: "Account is locked out. Please try again later.",
                statusCode: StatusCodes.Status429TooManyRequests
            );

        if (!result.Succeeded)
            return Results.Unauthorized();

        var domainUser = dbContext.DomainUsers.FirstOrDefault(u =>
            u.IdentityUserId == identityUser.Id
        );

        if (domainUser is null)
            return Results.Problem("User data inconsistency.", statusCode: 500);

        var accessToken = GenerateJwtToken(identityUser, domainUser, configuration);
        var refreshToken = new RefreshToken(domainUser.Id, RefreshTokenLifetime);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            BuildAuthResponse(accessToken, refreshToken.Token, identityUser.Email!, identityUser.DisplayName, domainUser.Id)
        );
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        PrivestioDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        var existingToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken is null)
            return Results.Unauthorized();

        // If someone tries to use a revoked token, it may indicate token theft.
        // Revoke all tokens for the user as a precaution.
        if (existingToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(dbContext, existingToken.UserId, cancellationToken);
            return Results.Unauthorized();
        }

        if (existingToken.IsExpired)
            return Results.Unauthorized();

        // Look up the user
        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);
        if (domainUser is null)
            return Results.Unauthorized();

        var identityUser = await userManager.FindByIdAsync(domainUser.IdentityUserId!);
        if (identityUser is null)
            return Results.Unauthorized();

        // Rotate: revoke the old token, issue a new pair
        var newRefreshToken = new RefreshToken(domainUser.Id, RefreshTokenLifetime);
        existingToken.Revoke(replacedByToken: newRefreshToken.Token);
        dbContext.RefreshTokens.Add(newRefreshToken);

        var accessToken = GenerateJwtToken(identityUser, domainUser, configuration);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(
            BuildAuthResponse(accessToken, newRefreshToken.Token, identityUser.Email!, identityUser.DisplayName, domainUser.Id)
        );
    }

    private static async Task<IResult> RevokeAsync(
        [FromBody] RefreshTokenRequest request,
        PrivestioDbContext dbContext,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var existingToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == request.RefreshToken && rt.UserId == userId,
                cancellationToken
            );

        if (existingToken is null || !existingToken.IsActive)
            return Results.NotFound();

        existingToken.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task RevokeAllUserTokensAsync(
        PrivestioDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AuthResponse BuildAuthResponse(
        string accessToken,
        string refreshToken,
        string email,
        string displayName,
        Guid userId
    )
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (int)AccessTokenLifetime.TotalSeconds,
            Email = email,
            DisplayName = displayName,
            UserId = userId,
        };
    }

    private static string GenerateJwtToken(
        ApplicationUser identityUser,
        User domainUser,
        IConfiguration configuration
    )
    {
        var jwtKey =
            configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.Add(AccessTokenLifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new Claim(JwtRegisteredClaimNames.Email, identityUser.Email!),
            new Claim("domain_user_id", domainUser.Id.ToString()),
            new Claim("display_name", identityUser.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
