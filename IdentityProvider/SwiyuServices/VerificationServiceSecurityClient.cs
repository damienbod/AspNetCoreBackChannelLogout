using Duende.IdentityModel.Client;
using Microsoft.Identity.Client;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;
public class VerificationServiceSecurityClient
{
    /// <summary>
    /// MSAL
    /// </summary>
    public static async Task<string> RequestTokenAsync(IConfiguration configuration)
    {
        // 1. Client client credentials client
        var app = ConfidentialClientApplicationBuilder
            .Create(configuration["SwiyuManagementClientId"])
            .WithClientSecret(configuration["SwiyuManagementClientSecretEntra"])
            .WithAuthority(configuration["SwiyuManagementAuthority"])
            .Build();

        var scopes = new[] { configuration["SwiyuManagementScope"] };

        // 2. Get access token
        var authResult = await app.AcquireTokenForClient(scopes)
            .ExecuteAsync();

        return authResult.AccessToken;

    }

    /// <summary>
    /// OAuth
    /// </summary>
    public static async Task<TokenResponse> RequestTokenOAuthAsync(IConfiguration configuration)
    {
        var client = new HttpClient();

        var disco = await client.GetDiscoveryDocumentAsync(configuration["OAuthIssuerUrl"]);

        if (disco.IsError) throw new Exception(disco.Error);

        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "swiyu-client",
            // Client assertions are better
            ClientSecret = "--from secrets vault--",
            Scope = "swiyu",
        });

        if (response.IsError) throw new Exception(response.Error);

        return response;
    }
}
