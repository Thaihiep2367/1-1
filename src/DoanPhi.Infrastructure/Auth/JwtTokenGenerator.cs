using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoanPhi.Application.Common.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DoanPhi.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    public const string SecretKey = "EduHub_Super_Secret_Key_For_Jwt_Authentication_2026_Secure_Token";
    public const string Issuer = "EduHub.DoanPhi";
    public const string Audience = "EduHub.Client";

    public string GenerateToken(string username, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role)
            ]),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
