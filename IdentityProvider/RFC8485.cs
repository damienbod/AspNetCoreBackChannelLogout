namespace Idp.Swiyu.Passkeys.Sts;

/// <summary>
/// Vectors of Trust (VoT) constants as defined in RFC 8485
/// https://www.rfc-editor.org/rfc/rfc8485.html
/// </summary>
public static class RFC8485
{
    /// <summary>
    /// Vector of Trust claim type
    /// </summary>
    public const string VOT = "vot";

    #region A.1. Identity Proofing (P)

    /// <summary>
    /// P0: No proofing is done, and data is not guaranteed to be persistent across sessions
    /// </summary>
    public const string P0 = "P0";

    /// <summary>
    /// P1: Attributes are self-asserted but consistent over time, potentially pseudonymous
    /// </summary>
    public const string P1 = "P1";

    /// <summary>
    /// P2: Identity has been proofed either in person or remotely using trusted mechanisms (such as social proofing)
    /// </summary>
    public const string P2 = "P2";

    /// <summary>
    /// P3: There is a binding relationship between the identity provider and the identified party
    /// (such as signed/notarized documents and employment records)
    /// </summary>
    public const string P3 = "P3";

    #endregion

    #region A.2. Primary Credential Usage (C)

    /// <summary>
    /// C0: No credential is used / anonymous public service
    /// </summary>
    public const string C0 = "C0";

    /// <summary>
    /// Ca: Simple session HTTP cookies (with nothing else)
    /// </summary>
    public const string Ca = "Ca";

    /// <summary>
    /// Cb: Known device, such as those indicated through device posture or device management systems
    /// </summary>
    public const string Cb = "Cb";

    /// <summary>
    /// Cc: Shared secret, such as a username and password combination
    /// </summary>
    public const string Cc = "Cc";

    /// <summary>
    /// Cd: Cryptographic proof of key possession using shared key
    /// </summary>
    public const string Cd = "Cd";

    /// <summary>
    /// Ce: Cryptographic proof of key possession using asymmetric key
    /// </summary>
    public const string Ce = "Ce";

    /// <summary>
    /// Cf: Sealed hardware token / keys stored in a trusted platform module
    /// </summary>
    public const string Cf = "Cf";

    /// <summary>
    /// Cg: Locally verified biometric
    /// </summary>
    public const string Cg = "Cg";

    #endregion

    #region A.3. Primary Credential Management (M)

    /// <summary>
    /// Ma: Self-asserted primary credentials (user chooses their own credentials and must rotate or revoke them manually)
    /// / no additional verification for primary credential issuance or rotation
    /// </summary>
    public const string Ma = "Ma";

    /// <summary>
    /// Mb: Remote issuance and rotation / use of backup recovery credentials (such as email verification)
    /// / deletion on user request
    /// </summary>
    public const string Mb = "Mb";

    /// <summary>
    /// Mc: Full proofing required for each issuance and rotation / revocation on suspicious activity
    /// </summary>
    public const string Mc = "Mc";

    #endregion

    #region A.4. Assertion Presentation (A)

    /// <summary>
    /// Aa: No protection / unsigned bearer identifier (such as an HTTP session cookie in a web browser)
    /// </summary>
    public const string Aa = "Aa";

    /// <summary>
    /// Ab: Signed and verifiable assertion, passed through the user agent (web browser)
    /// </summary>
    public const string Ab = "Ab";

    /// <summary>
    /// Ac: Signed and verifiable assertion, passed through a back channel
    /// </summary>
    public const string Ac = "Ac";

    /// <summary>
    /// Ad: Assertion encrypted to the RP's key
    /// </summary>
    public const string Ad = "Ad";

    #endregion

    /// <summary>
    /// P: OpenIDVP | C: Passkeys
    /// Example: "P3.Cf.Ma.Ac"
    /// </summary>
    public const string OpenIDVP_Passkeys = "P3.Cf.Ma.Ac";

    /// <summary>
    /// P: Email | C: Passkeys
    /// </summary>
    public const string Email_Passkeys = "P1.Cf.Ma.Ac";

    /// <summary>
    /// P: Email | C: Password
    /// Example: "P1.Cc.Ma.Ac"
    /// </summary>
    public const string P_Email_C_Password = "P1.Cc.Ma.Ac";

    /// <summary>
    /// P: OpenIDVP | C: Password
    /// Example: "P3.Cc.Ma.Ac"
    /// </summary>
    public const string P_OpenIDVP_C_Password = "P3.Cc.Ma.Ac";

    /// <summary>
    /// P: OpenIDVP | C: OpenIDVP
    /// Example: "P3.Ce.Ma.Ac"
    /// </summary>
    public const string P_OpenIDVP_C_OpenIDVP = "P3.Ce.Ma.Ac";

    /// <summary>
    /// P: OpenIDVP | C: Password, OpenIDVP
    /// Example: "P3.Ce.Cc.Ma.Ac"
    /// </summary>
    public const string P_OpenIDVP_C_Password_OpenIDVP = "P3.Ce.Cc.Ma.Ac";

    /// <summary>
    /// P: OpenIDVP | C: Password, passkeys
    /// Example: "P3.Cf.Cc.Ma.Ac"
    /// </summary>
    public const string P_OpenIDVP_C_Password_Passkeys = "P3.Cf.Cc.Ma.Ac";

    /// <summary>
    /// Builds a Vector of Trust string from individual components
    /// </summary>
    /// <param name="identityProofing">Identity Proofing level (P0-P3)</param>
    /// <param name="credentialUsage">Primary Credential Usage (C0, Ca-Cg)</param>
    /// <param name="credentialManagement">Primary Credential Management (Ma-Mc)</param>
    /// <param name="assertionPresentation">Assertion Presentation (Aa-Ad)</param>
    /// <returns>VoT string in format "P.C.M.A"</returns>
    public static string BuildVoT(string identityProofing, string credentialUsage, 
        string credentialManagement, string assertionPresentation)
    {
        return $"{identityProofing}.{credentialUsage}.{credentialManagement}.{assertionPresentation}";
    }

    /// <summary>
    /// Builds a Vector of Trust string with multiple credential usage methods
    /// </summary>
    /// <param name="identityProofing">Identity Proofing level (P0-P3)</param>
    /// <param name="credentialUsages">Array of Primary Credential Usage methods</param>
    /// <param name="credentialManagement">Primary Credential Management (Ma-Mc)</param>
    /// <param name="assertionPresentation">Assertion Presentation (Aa-Ad)</param>
    /// <returns>VoT string in format "P.C1.C2.M.A"</returns>
    public static string BuildVoT(string identityProofing, string[] credentialUsages, 
        string credentialManagement, string assertionPresentation)
    {
        var credentials = string.Join(".", credentialUsages);
        return $"{identityProofing}.{credentials}.{credentialManagement}.{assertionPresentation}";
    }

    /// <summary>
    /// Builds a Vector of Trust (VoT) value from LOI (Level of Identification), 
    /// LOA (Level of Authentication), and AMR (Authentication Method Reference) values.
    /// </summary>
    /// <param name="loi">Level of Identification (e.g., "loi.100", "loi.400", "loi.500")</param>
    /// <param name="loa">Level of Authentication (e.g., "loa.100", "loa.400")</param>
    /// <param name="amr">Authentication Method Reference (e.g., "pwd", "pop", "hwk")</param>
    /// <returns>VoT string in RFC 8485 format (e.g., "P3.Cf.Ma.Ac")</returns>
    public static string BuildVoTFromLoiLoaAmr(string loi, string loa, string amr)
    {
        // Map LOI to Identity Proofing (P)
        var identityProofing = loi switch
        {
            Consts.LOI_500 => P3, // Offline Human identification by trusted official
            Consts.LOI_400 => P3, // OpenID verifiable credentials (E-ID, swiyu), government issued
            Consts.LOI_300 => P2, // Digital online check with person
            Consts.LOI_200 => P1, // Digital video without person
            Consts.LOI_100 => P1, // Email & SMS validation
            _ => P1 // Default to self-asserted
        };

        // Map AMR to Primary Credential Usage (C)
        var credentialUsage = amr switch
        {
            Amr.Pop => Cf,   // Proof-of-possession (passkeys) -> Sealed hardware token
            Amr.Hwk => Cf,   // Hardware key (passkeys) -> Sealed hardware token
            Amr.Swk => Ce,   // Software key -> Asymmetric key
            Amr.Pwd => Cc,   // Password -> Shared secret
            Amr.Otp => Cc,   // One-time password -> Shared secret
            Amr.Sms => Cc,   // SMS -> Shared secret
            Amr.Pin => Cc,   // PIN -> Shared secret
            Amr.Face => Cg,  // Facial recognition -> Locally verified biometric
            Amr.Fpt => Cg,   // Fingerprint -> Locally verified biometric
            Amr.Iris => Cg,  // Iris scan -> Locally verified biometric
            Amr.Retina => Cg, // Retina scan -> Locally verified biometric
            Amr.Vbm => Cg,   // Voice biometric -> Locally verified biometric
            Amr.Mfa => Cf,   // Multi-factor -> Sealed hardware token (strongest)
            _ => Cc // Default to shared secret
        };

        // Credential Management - defaults to Ma (self-asserted)
        // Can be overridden based on LOI level for stricter management
        var credentialManagement = loi switch
        {
            Consts.LOI_500 => Mc, // Full proofing required
            Consts.LOI_400 => Mc, // Full proofing required
            _ => Ma // Self-asserted
        };

        // Assertion Presentation - defaults to Ac (back channel)
        // This is typical for OpenID Connect with back-channel token endpoint
        var assertionPresentation = Ac;

        return BuildVoT(identityProofing, credentialUsage, credentialManagement, assertionPresentation);
    }

    /// <summary>
    /// Builds a Vector of Trust (VoT) value from LOI, LOA, and multiple AMR values.
    /// Useful when multiple authentication methods were used (e.g., password + passkey).
    /// </summary>
    /// <param name="loi">Level of Identification</param>
    /// <param name="loa">Level of Authentication</param>
    /// <param name="amrValues">Array of Authentication Method References</param>
    /// <returns>VoT string with multiple credential usage methods</returns>
    public static string BuildVoTFromLoiLoaAmr(string loi, string loa, string[] amrValues)
    {
        // Map LOI to Identity Proofing
        var identityProofing = loi switch
        {
            Consts.LOI_500 => P3,
            Consts.LOI_400 => P3,
            Consts.LOI_300 => P2,
            Consts.LOI_200 => P2,
            Consts.LOI_100 => P1,
            _ => P1
        };

        // Map each AMR to Credential Usage
        var credentialUsages = amrValues.Select(amr => amr switch
        {
            Amr.Pop => Cf,
            Amr.Hwk => Cf,
            Amr.Swk => Ce,
            Amr.Pwd => Cc,
            Amr.Otp => Cc,
            Amr.Sms => Cc,
            Amr.Pin => Cc,
            Amr.Face => Cg,
            Amr.Fpt => Cg,
            Amr.Iris => Cg,
            Amr.Retina => Cg,
            Amr.Vbm => Cg,
            Amr.Mfa => Cf,
            _ => Cc
        }).Distinct().ToArray();

        var credentialManagement = loi switch
        {
            Consts.LOI_500 => Mc,
            Consts.LOI_400 => Mc,
            _ => Ma
        };

        var assertionPresentation = Ac;

        return BuildVoT(identityProofing, credentialUsages, credentialManagement, assertionPresentation);
    }
}