using System.Text.Json.Serialization;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;

public class VerificationWalletResponseModel
{
    [JsonPropertyName("error_code")]
    public string? error_code { get; set; }

    [JsonPropertyName("error_description")]
    public string? error_description { get; set; }

    [JsonPropertyName("credential_subject_data")]
    public object? credential_subject_data { get; set; }
}