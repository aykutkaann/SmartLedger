using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;



namespace SmartLedger.Application.Auth
{
    //DTOs

    public record AuthResponse(string AccessToken, string RefreshToken, string Email, string Role);

    //Register

    public record RegisterCommand(string Email, string Password) : IRequest<AuthResponse>;

    public class RegisterHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow,
        IConfiguration config) : IRequestHandler<RegisterCommand, AuthResponse>
    {
        public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            if (await users.GetByEmailAsync(cmd.Email, ct) is not null)
                throw new InvalidOperationException("Email already registered.");

            var hash = BCrypt.Net.BCrypt.HashPassword(cmd.Password, workFactor: 12);
            var user = User.Create(cmd.Email, hash);
            await users.AddAsync(user, ct);

            var (accessToken, rawRefresh) = TokenHelpers.GenerateTokens(user, config);
            var refreshHash = TokenHelpers.HashToken(rawRefresh);
            await refreshTokens.AddAsync(RefreshToken.Create(user.Id, refreshHash), ct);

            await uow.SaveChangesAsync(ct);
            return new AuthResponse(accessToken, rawRefresh, user.Email, user.Role);
        }
    }


    // ---Login

    public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

    public class LoginHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow,
        IConfiguration config) : IRequestHandler<LoginCommand, AuthResponse>
    {

        public async Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken ct)
        {
            var user = await users.GetByEmailAsync(cmd.Email, ct) ?? throw new UnauthorizedAccessException("Invalid credentials.");


            if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            await refreshTokens.RevokeAllForUserAsync(user.Id, ct);

            var (accessToken, rawRefresh) = TokenHelpers.GenerateTokens(user, config);
            await refreshTokens.AddAsync(RefreshToken.Create(user.Id, TokenHelpers.HashToken(rawRefresh)), ct);

            await uow.SaveChangesAsync(ct);
            return new AuthResponse(accessToken, rawRefresh, user.Email, user.Role);

        }

    }

    // ── Refresh
    public record RefreshCommand(string RefreshToken) : IRequest<AuthResponse>;

    public class RefreshHandler(
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow,
        IConfiguration config) : IRequestHandler<RefreshCommand, AuthResponse>
    {
        public async Task<AuthResponse> Handle(RefreshCommand cmd, CancellationToken ct)
        {
            var hash = TokenHelpers.HashToken(cmd.RefreshToken);
            var token = await refreshTokens.GetByHashAsync(hash, ct)
                ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            if (!token.IsActive)
                throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

            

            token.Revoke();

            var (accessToken, rawRefresh) = TokenHelpers.GenerateTokens(token.User, config);
            await refreshTokens.AddAsync(RefreshToken.Create(token.User.Id, TokenHelpers.HashToken(rawRefresh)), ct);

            await uow.SaveChangesAsync(ct);
            return new AuthResponse(accessToken, rawRefresh, token.User.Email, token.User.Role);
        }
    }



    // ── Shared helpers 
    internal static class TokenHelpers
    {
        internal static (string AccessToken, string RawRefresh) GenerateTokens(User user, IConfiguration config)
        {
            var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
            var jwtIssuer = config["Jwt:Issuer"] ?? "SmartLedger";
            var expMins = int.Parse(config["Jwt:ExpiryMinutes"] ?? "15");

            var claims = new[]
     {
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) 
};

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expMins),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            return (accessToken, rawRefresh);
        }

        internal static string HashToken(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
