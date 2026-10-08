using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;

/// <summary>
/// Issuer: If the JWT based authentication is activated, all calls must be wrapped in a signed JWT with the claim "data" other calls will be rejected. 
/// The value of the data claim will contain the full json body of the normal request.
///
/// Note that this is only affects writing calls.
/// https://github.com/swiyu-admin-ch/swiyu-issuer?tab=readme-ov-file#security
/// https://github.com/swiyu-admin-ch/swiyu-verifier?tab=readme-ov-file#security
/// </summary>
public static class SignSwiyuManagementPayload
{
    public static string SignRequest(IConfiguration configuration, string data)
    {
        var now = DateTime.UtcNow;

        var privatePem = ConfigConverter.GetPemFromBase64Config("WebDpopClientPrivatePemBase64", configuration);
        var publicPem = ConfigConverter.GetPemFromBase64Config("WebDpopClientPublicPemBase64", configuration);

        var ecCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
        var ecCertificateKey = new ECDsaSecurityKey(ecCertificate.GetECDsaPrivateKey());
        var signingCredentials = new SigningCredentials(new X509SecurityKey(ecCertificate), "ES384");

        var claims = new List<Claim>
        {
            new Claim("data", data)
        };

        var token = new JwtSecurityToken(
            null,
            null,
            claims,
            now,
            now.AddMinutes(1),
            signingCredentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        return tokenHandler.WriteToken(token);
    }
}
