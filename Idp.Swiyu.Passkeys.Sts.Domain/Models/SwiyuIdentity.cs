namespace Idp.Swiyu.Passkeys.Sts.Domain.Models;

public class SwiyuIdentity
{
    public int Id { get; set; }

    public string GivenName { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string BirthPlace { get; set; } = null!;
    public string BirthDate { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
}