using System.Text;

namespace Idp.Swiyu.Passkeys.Sts;

public static class ConfigConverter
{
    public static string GetPemFromBase64Config(string config, IConfiguration configuration)
    {
        var base64String = configuration.GetValue<string>(config);

        if (string.IsNullOrEmpty(base64String))
        {
            throw new ArgumentException($"PEM Configuration value for '{config}' is missing or empty.");
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }

    public static string GetPemFromBase64(string base64String)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }

    public static string CreateBase64FromPem(string pem)
    {
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));

        return base64String;
    }
}

