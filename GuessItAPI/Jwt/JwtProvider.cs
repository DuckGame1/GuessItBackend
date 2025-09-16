using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GuessItAPI.Jwt
{
    public static class JwtProvider
    {
        private readonly static string secretKey = "QffgUFbwcuC27#E23!@#*HDC3HDg0238gD^@0eh)DG2407G)!*#67e%X2";
        private readonly static int expiresHours = 1200;
        public static JwtOptions options = new JwtOptions() { SecretKey = secretKey, ExpiresHours = expiresHours};
        
        public static string GenerateToken(Models.User user, bool isUnlimited)
        {
            Claim[] claims = [new("userId", user.UserId.ToString()), new(ClaimTypes.Name, user.Username), new(ClaimTypes.Role, user.Role)];

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SecretKey)), SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token;
            if (isUnlimited)
            {
                token = new JwtSecurityToken(
                    signingCredentials: signingCredentials,
                    claims: claims,
                    expires: DateTime.UtcNow.AddYears(20));
            }
            else
            {
                token = new JwtSecurityToken(
                    signingCredentials: signingCredentials,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(options.ExpiresHours));
            }

            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenValue;
        }
    }
}
