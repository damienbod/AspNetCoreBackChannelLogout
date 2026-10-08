namespace Idp.Swiyu.Passkeys.Sts;

/// <summary>
/// https://tools.ietf.org/html/draft-ietf-oauth-amr-values-04
/// https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
/// </summary>
public static class Amr
{
    /// <summary>
    /// Jones, et al.Expires May 17, 2017
    /// Internet-Draft Authentication Method Reference Values November 2016
    /// Facial recognition
    /// </summary>
    public const string Face = "face";

    /// <summary>
    /// Fingerprint biometric
    /// </summary>
    public const string Fpt = "fpt";

    /// <summary>
    /// Use of geolocation information
    /// </summary>
    public const string Geo = "geo";

    /// <summary>
    /// Proof-of-possession(PoP) of a hardware-secured key.See
    /// Appendix C of[RFC4211] for a discussion on PoP.
    /// </summary>
    public const string Hwk = "hwk";

    /// <summary>
    /// Iris scan biometric
    /// </summary>
    public const string Iris = "iris";

    /// <summary>
    /// Knowledge-based authentication [NIST.800-63-2] [ISO29115]
    /// </summary>
    public const string Kba = "kba";

    /// <summary>
    /// Multiple-channel authentication.  The authentication involves
    /// communication over more than one distinct communication channel.
    /// For instance, a multiple-channel authentication might involve both
    /// entering information into a workstation's browser and providing
    /// information on a telephone call to a pre-registered number.
    /// </summary>
    public const string Mca = "mca";

    /// <summary>
    /// Multiple-factor authentication [NIST.800-63-2]  [ISO29115].  When 
    /// this is present, specific authentication methods used may also be
    /// included.
    /// </summary>
    public const string Mfa = "mfa";

    /// <summary>
    /// One-time password.  One-time password specifications that this
    /// authentication method applies to include[RFC4226] and[RFC6238].
    /// </summary>
    public const string Otp = "otp";

    /// <summary>
    /// Personal Identification Number or pattern (not restricted to
    /// containing only numbers) that a user enters to unlock a key on the
    /// device.This mechanism should have a way to deter an attacker
    /// from obtaining the PIN by trying repeated guesses.
    /// </summary>
    public const string Pin = "pin";

    /// <summary>
    /// Password-based authentication
    /// </summary>
    public const string Pwd = "pwd";

    /// <summary>
    /// Risk-based authentication [JECM]
    /// </summary>
    public const string Rba = "rba";

    /// <summary>
    /// Retina scan biometric Jones, et al.Expires May 17, 2017
    /// Internet-Draft Authentication Method Reference Values November 2016
    /// </summary>
    public const string Retina = "retina";

    /// <summary>
    /// Smart card
    /// </summary>
    public const string Sc = "sc";

    /// <summary>
    /// Confirmation using SMS message to the user at a registered number
    /// </summary>
    public const string Sms = "sms";

    /// <summary>
    /// Proof-of-possession(PoP) of a software-secured key.See
    /// Appendix C of[RFC4211] for a discussion on PoP.
    /// </summary>
    public const string Swk = "swk";

    /// <summary>
    /// Confirmation by telephone call to the user at a registered number
    /// </summary>
    public const string Tel = "tel";

    /// <summary>
    /// User presence test
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// Voice biometric
    /// </summary>
    public const string Vbm = "vbm";

    /// <summary>
    /// Windows integrated authentication, as described in [MSDN]
    /// </summary>
    public const string Wia = "wia";

    /// <summary>
    /// Proof-of-possession of a key. Unlike the existing hwk and swk methods, it is unspecified whether the proof-of-possession key is hardware-secured or software-secured.
    /// </summary>
    public const string Pop = "pop";
}