namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;

public class VerificationClaims
{
    public string GivenName { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string BirthPlace { get; set; } = null!;
    public string BirthDate { get; set; } = null!;
}