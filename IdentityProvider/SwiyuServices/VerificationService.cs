using Duende.IdentityModel.Client;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;

public class VerificationService
{
    private readonly ILogger<VerificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _swiyuVerifierMgmtUrl;
    private readonly string? _issuerId;
    private readonly HttpClient _httpClient;

    private const string SWIYU_BETA_ID = "swiyu-beta-id";

    public VerificationService(IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _swiyuVerifierMgmtUrl = configuration["SwiyuVerifierMgmtUrl"];
        _issuerId = configuration["ISSUER_ID"];
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<VerificationService>();
        _configuration = configuration;
    }

    /// <summary>
    /// curl - X POST http://localhost:8082/management/api/verifications \
    ///       -H "accept: application/json" \
    ///       -H "Content-Type: application/json" \
    ///       -d '
    /// </summary>
    public async Task<string> CreateBetaIdVerificationPresentationAsync()
    {
        _logger.LogInformation("Creating verification presentation");

        // from "betaid-sdjwt"
        var acceptedIssuerDid = "did:tdw:QmPEZPhDFR4nEYSFK5bMnvECqdpf1tPTPJuWs9QrMjCumw:identifier-reg.trust-infra.swiyu-int.admin.ch:api:v1:did:9a5559f0-b81c-4368-a170-e7b4ae424527";

        var presentationDefinitionId = SWIYU_BETA_ID;

        var json = GetBetaIdVerificationPresentationBodyV4(presentationDefinitionId, acceptedIssuerDid);

        // TODO sign the payload if JWT authentication is enabled on Swiyu  

        return await SendCreateVerificationPostRequest(json);
    }

    public async Task<VerificationManagementModel?> GetVerificationStatus(string verificationId)
    {
        var accessToken = await VerificationServiceSecurityClient.RequestTokenAsync(_configuration);
        _httpClient.SetBearerToken(accessToken);

        var idEncoded = HttpUtility.UrlEncode(verificationId);
        using HttpResponseMessage response = await _httpClient.GetAsync(
            $"{_swiyuVerifierMgmtUrl}/management/api/verifications/{idEncoded}");

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (jsonResponse == null)
            {
                _logger.LogError("GetVerificationStatus no data returned from Swiyu");
                return null;
            }
            else if(jsonResponse.Contains("FAILED"))
            {
                _logger.LogInformation("GetVerificationStatus verificationId FAILED: {jsonResponse}", jsonResponse);
                return null;
            }

            //  state: PENDING, SUCCESS, FAILED
            return JsonSerializer.Deserialize<VerificationManagementModel>(jsonResponse);
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Could not create verification presentation {vp}", error);

        throw new ArgumentException(error);
    }

    /// <summary>
    /// In a business app we can use the data from the verificationModel
    /// Verification data:
    /// Use: wallet_response/credential_subject_data
    ///
    /// birth_date, given_name, family_name, birth_place
    /// 
    /// </summary>
    /// <param name="verificationManagementModel"></param>
    /// <returns></returns>
    public VerificationClaims GetVerifiedClaims(VerificationManagementModel verificationManagementModel)
    {
        var json = verificationManagementModel.wallet_response?.credential_subject_data?.ToString();

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Missing credential_subject_data in wallet_response.");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("credential_subject_data must be a JSON object.");
        }

        var hasCredentialEntries =
            TryGetCredentialEntries(root, verificationManagementModel.id, out var credentialEntries) ||
            TryGetCredentialEntries(root, SWIYU_BETA_ID, out credentialEntries) ||
            TryGetFirstCredentialEntries(root, out credentialEntries);

        if (!hasCredentialEntries)
        {
            throw new ArgumentException($"No credential_subject_data found for verification id '{verificationManagementModel.id}'.");
        }

        var claimSource = credentialEntries[0];

        var claims = new VerificationClaims
        {
            BirthDate = claimSource.GetProperty("birth_date").GetString()!,
            BirthPlace = claimSource.GetProperty("birth_place").GetString()!,
            FamilyName = claimSource.GetProperty("family_name").GetString()!,
            GivenName = claimSource.GetProperty("given_name").GetString()!
        };

        return claims;
    }

    private static bool TryGetCredentialEntries(JsonElement root, string? key, out JsonElement credentialEntries)
    {
        credentialEntries = default;

        if (string.IsNullOrWhiteSpace(key) || !root.TryGetProperty(key, out var entries))
        {
            return false;
        }

        if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() == 0)
        {
            return false;
        }

        credentialEntries = entries;
        return true;
    }

    private static bool TryGetFirstCredentialEntries(JsonElement root, out JsonElement credentialEntries)
    {
        credentialEntries = default;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array && property.Value.GetArrayLength() > 0)
            {
                credentialEntries = property.Value;
                return true;
            }
        }

        return false;
    }

    private async Task<string> SendCreateVerificationPostRequest(string json)
    {
        var accessToken = await VerificationServiceSecurityClient.RequestTokenAsync(_configuration);

        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.SetBearerToken(accessToken);
        var response = await _httpClient.PostAsync($"{_swiyuVerifierMgmtUrl}/management/api/verifications", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Could not create verification presentation {vp}", error);

        throw new ArgumentException(error);
    }

    /// <summary>
    /// > **Note:** The verifier accepts both `dc+sd-jwt` (current spec, SD-JWT VC Draft 06+, per [draft-ietf-oauth-sd-jwt-vc-09 §A.2.1](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-sd-jwt-vc-09#name-application-dcsd-jwt)) 
    /// and `vc+sd-jwt` (legacy SD-JWT VC drafts ≤ 05) on the credential's `typ` header.
    /// There will be private companies having a need to do identification routines (e.g. KYC or before issuing another credential), 
    /// asking for given_name, family_name, birth_date and birth_place.
    /// 
    /// { "path": [ "$.birth_date" ] },
    /// { "path": ["$.given_name"] },
    /// { "path": ["$.family_name"] },
    /// { "path": ["$.birth_place"] },
    /// </summary>
    private static string GetBetaIdVerificationPresentationBodyV4(string presentationDefinitionId, string acceptedIssuerDid)
    {
        var json = $$"""
             {
                 "accepted_issuer_dids": [ "{{acceptedIssuerDid}}" ],
                 "jwt_secured_authorization_request": true,
                 "response_mode": "direct_post.jwt",
                 "verification_purpose": {
                   "scope": "ch.identity",
                   "purpose_name": {
                     "default": "Identity verification"
                   },
                   "purpose_description": {
                     "default": "Used to verify the identity of an individual"
                   }
                 },
                 "dcql_query": {
                   "credentials": [
                     {
                       "id": "{{presentationDefinitionId}}",
                       "format": "dc+sd-jwt",
                       "meta": {
                         "vct_values": ["urn:vct:ch.admin.bcs.betaid"]
                       },
                       "claims": [
                         { "path": [ "birth_date" ] },
                         { "path": [ "given_name" ] },
                         { "path": [ "family_name" ] },
                         { "path": [ "birth_place" ] }
                       ],
                       "require_cryptographic_holder_binding": true
                     }
                   ]
                 }
             }
             """;

        return json;
    }
}
