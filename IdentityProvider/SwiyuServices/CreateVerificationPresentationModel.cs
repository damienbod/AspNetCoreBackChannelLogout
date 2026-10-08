using System.Text.Json.Serialization;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;

public class CreateVerificationPresentationModel
{
    [JsonPropertyName("id")]
    public string id { get; set; } = string.Empty;
    [JsonPropertyName("request_nonce")]
    public string request_nonce { get; set; } = string.Empty;
    [JsonPropertyName("state")]
    public string state { get; set; } = string.Empty;
    [JsonPropertyName("presentation_definition")]
    public object? presentation_definition { get; set; }
    [JsonPropertyName("verification_url")]
    public string verification_url { get; set; } = string.Empty;
    [JsonPropertyName("verification_deeplink")]
    public string verification_deeplink { get; set; } = string.Empty;
}