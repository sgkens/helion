namespace Helion.Host.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Helion.Host.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HelionConfig _config;

    public JwtAuthMiddleware(RequestDelegate next, HelionConfig config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 20-Pass Fix: Stateless JWT Auth
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                // Note: In a real system, the secret would be loaded from a secure vault as per Pass 5
                var key = Encoding.ASCII.GetBytes("helion_master_secret_placeholder_32_chars"); 
                
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var tenantId = jwtToken.Claims.First(x => x.Type == "tenant_id").Value;

                // Inject headers for Go Engine (Pass 5 mandate)
                context.Request.Headers.Append("X-Helion-Tenant-Id", tenantId);
                
                // Set User for internal Host logic
                context.Items["User"] = tenantId;
            }
            catch (Exception)
            {
                // Token validation failed, but we continue to allow public endpoints or handle in downstream
            }
        }

        await _next(context);
    }
}
