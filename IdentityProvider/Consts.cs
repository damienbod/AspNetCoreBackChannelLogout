namespace Idp.Swiyu.Passkeys.Sts;

public static class Consts
{
    /// <summary>
    /// loa (Level of Authentication)
    /// </summary>
    public const string LOA = "loa";

    /// <summary>
    /// passkeys, (public/private key certificate authentication)
    /// </summary>
    public const string LOA_400 = "loa.400";

    /// <summary>
    /// authenticator apps, OpenID verifiable credentials(E-ID, swiyu)
    /// </summary>
    public const string LOA_300 = "loa.300";

    /// <summary>
    /// SMS, email, TOTP, 2-step
    /// </summary>
    public const string LOA_200 = "loa.200";

    /// <summary>
    /// single factor, SAS key, API Keys, passwords, OTP
    /// </summary>
    public const string LOA_100 = "loa.100";

    /// <summary>
    /// loi (Level of Identification)
    /// </summary>
    public const string LOI = "loi";

    /// <summary>
    /// Offline Human identification by trusted official in trustworthy organisation.
    /// </summary>
    public const string LOI_500 = "loi.500";

    /// <summary>
    /// OpenID verifiable credentials (E-ID, swiyu), government issued.
    /// </summary>
    public const string LOI_400 = "loi.400";

    /// <summary>
    /// Digital online check with person
    /// </summary>
    public const string LOI_300 = "loi.300";

    /// <summary>
    /// Digital video without person
    /// </summary>
    public const string LOI_200 = "loi.200";

    /// <summary>
    /// Email 
    /// </summary>& SMS validation, or none
    public const string LOI_100 = "loi.100";
}
