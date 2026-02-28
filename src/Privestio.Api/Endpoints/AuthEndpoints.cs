using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Infrastructure.Data;
using Privestio.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Privestio.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Login with email and password")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        PrivestioDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
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
            EmailConfirmed = true, // For now, auto-confirm
        };

        var createResult = await userManager.CreateAsync(identityUser, request.Password);
        if (!createResult.Succeeded)
        {
            return Results.ValidationProblem(
                createResult.Errors.ToDictionary(
                    e => e.Code,
                    e => new[] { e.Description }));
        }

        // Link identity user ID back to domain user
        domainUser.IdentityUserId = identityUser.Id;
        dbContext.DomainUsers.Add(domainUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(identityUser, domainUser, configuration);

        return Results.Created(
            $"/api/v1/users/{domainUser.Id}",
            new AuthResponse
            {
                AccessToken = token,
                ExpiresIn = 3600,
                Email = request.Email,
                DisplayName = request.DisplayName,
                UserId = domainUser.Id,
            });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        PrivestioDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email);
        if (identityUser is null)
            return Results.Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(
            identityUser,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Results.Problem(
                detail: "Account is locked out. Please try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);

        if (!result.Succeeded)
            return Results.Unauthorized();

        var domainUser = dbContext.DomainUsers
            .FirstOrDefault(u => u.IdentityUserId == identityUser.Id);

        if (domainUser is null)
            return Results.Problem("User data inconsistency.", statusCode: 500);

        var token = GenerateJwtToken(identityUser, domainUser, configuration);

        return Results.Ok(new AuthResponse
        {
            AccessToken = token,
            ExpiresIn = 3600,
            Email = identityUser.Email!,
            DisplayName = identityUser.DisplayName,
            UserId = domainUser.Id,
        });
    }

    private static string GenerateJwtToken(
        ApplicationUser identityUser,
        User domainUser,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
