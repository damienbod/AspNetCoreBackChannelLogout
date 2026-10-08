using Duende.IdentityModel;
using Idp.Swiyu.Passkeys.Sts.Domain;
using Idp.Swiyu.Passkeys.Sts.Domain.Models;
using Idp.Swiyu.Passkeys.Sts.SwiyuServices;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Net.Codecrete.QrCodeGenerator;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Swiyu;

[Authorize]
public class RegisterModel : PageModel
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IHttpClientFactory _clientFactory;
    private readonly VerificationService _verificationService;
    private readonly string? _swiyuOid4vpUrl;
    private readonly UserManager<ApplicationUser> _userManager;

    [BindProperty]
    public string? VerificationId { get; set; }

    [BindProperty]
    public string? QrCodeUrl { get; set; } = string.Empty;

    [BindProperty]
    public byte[] QrCodePng { get; set; } = [];

    [BindProperty]
    public string? DeepLink { get; set; } = string.Empty;

    public RegisterModel(VerificationService verificationService,
        IConfiguration configuration,
        IHttpClientFactory clientFactory,
        ApplicationDbContext applicationDbContext,
        UserManager<ApplicationUser> userManager)
    {
        _applicationDbContext = applicationDbContext;
        _clientFactory = clientFactory;
        _verificationService = verificationService;
        _swiyuOid4vpUrl = configuration["SwiyuOid4vpUrl"];
        QrCodeUrl = QrCodeUrl.Replace("{OID4VP_URL}", _swiyuOid4vpUrl);
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.FindByEmailAsync(GetEmail(User.Claims)!);
        var swiyuVerifiedIdentity = _applicationDbContext.SwiyuIdentity.FirstOrDefault(si => si.UserId == user!.Id);

        if (swiyuVerifiedIdentity != null)
        {
            // User already has a verified Swiyu identity, redirect to complete page
            Response.Redirect("/Swiyu/IdentityAlreadyVerified");
            return;
        }

        var presentation = await _verificationService
           .CreateBetaIdVerificationPresentationAsync();

        var verificationResponse = JsonSerializer.Deserialize<CreateVerificationPresentationModel>(presentation);
        // verification_url
        QrCodeUrl = verificationResponse!.verification_url;

        var qrCode = QrCode.EncodeText(verificationResponse!.verification_deeplink, QrCode.Ecc.Quartile);
        QrCodePng = qrCode.ToPng(20, 4, MagickColors.Black, MagickColors.White);

        DeepLink = verificationResponse.verification_deeplink;

        VerificationId = verificationResponse.id;
    }

    public async Task<IActionResult> OnPost()
    {
        VerificationClaims verificationClaims = null;
        try
        {
            if (VerificationId == null)
            {
                return BadRequest(new { error = "400", error_description = "Missing argument 'VerificationId'" });
            }

            var verificationModel = await RequestSwiyuClaimsAsync(1, VerificationId);

            verificationClaims = _verificationService.GetVerifiedClaims(verificationModel);

            var exists = _applicationDbContext.SwiyuIdentity.FirstOrDefault(c =>
                    c.BirthDate == verificationClaims.BirthDate &&
                    c.BirthPlace == verificationClaims.BirthPlace &&
                    c.GivenName == verificationClaims.GivenName &&
                    c.FamilyName == verificationClaims.FamilyName);

            if (exists != null)
            {
                var user = await _userManager.FindByIdAsync(exists.UserId);

                if (user == null)
                {
                    // This should return a user message with no info what went wrong.
                    throw new ArgumentNullException("error in authentication");
                }
            }
            else
            {
                await ConnectAccountWithIdentity(verificationClaims);
                return Redirect("/Swiyu/IdentityCheckComplete");
            }

        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }

        return Page();
    }

    internal async Task<VerificationManagementModel> RequestSwiyuClaimsAsync(int interval, string verificationId)
    {
        var client = _clientFactory.CreateClient();

        while (true)
        {

            var verificationModel = await _verificationService.GetVerificationStatus(verificationId);

            if (verificationModel != null && verificationModel.state == "SUCCESS")
            {
                return verificationModel;
            }
            else
            {
                await Task.Delay(interval * 1000);
            }
        }
    }

    private async Task ConnectAccountWithIdentity(VerificationClaims verificationClaims)
    {
        var user = await _userManager.FindByEmailAsync(GetEmail(User.Claims)!);

        var exists = _applicationDbContext.SwiyuIdentity.FirstOrDefault(c =>
            c.BirthDate == verificationClaims.BirthDate &&
            c.BirthPlace == verificationClaims.BirthPlace &&
            c.GivenName == verificationClaims.GivenName &&
            c.FamilyName == verificationClaims.FamilyName);

        if (exists != null)
        {
            throw new Exception("swiyu already in use and connected to an account...");
        }

        if (user != null && (user.SwiyuIdentityId == null || user.SwiyuIdentityId <= 0))
        {
            var swiyuIdentity = new SwiyuIdentity
            {
                UserId = user.Id,
                BirthDate = verificationClaims.BirthDate,
                FamilyName = verificationClaims.FamilyName,
                BirthPlace = verificationClaims.BirthPlace,
                GivenName = verificationClaims.GivenName,
                Email = user.Email!
            };

            _applicationDbContext.SwiyuIdentity.Add(swiyuIdentity);

            // Save to DB
            user.SwiyuIdentityId = swiyuIdentity.Id;
            await _applicationDbContext.SaveChangesAsync();

            // remove demo claims
            await _userManager.RemoveClaimsAsync(user, await _userManager.GetClaimsAsync(user));
        }
    }

    public static string? GetEmail(IEnumerable<Claim> claims)
    {
        var email = claims.FirstOrDefault(t => t.Type == ClaimTypes.Email);

        if (email != null)
        {
            return email.Value;
        }

        email = claims.FirstOrDefault(t => t.Type == JwtClaimTypes.Email);

        if (email != null)
        {
            return email.Value;
        }

        email = claims.FirstOrDefault(t => t.Type == "preferred_username");

        if (email != null)
        {
            var isNameAndEmail = IsEmailValid(email.Value);
            if (isNameAndEmail)
            {
                return email.Value;
            }
        }

        return null;
    }

    public static bool IsEmailValid(string email)
    {
        if (!MailAddress.TryCreate(email, out var mailAddress))
            return false;

        // And if you want to be more strict:
        var hostParts = mailAddress.Host.Split('.');
        if (hostParts.Length == 1)
            return false; // No dot.
        if (hostParts.Any(p => p == string.Empty))
            return false; // Double dot.
        if (hostParts[^1].Length < 2)
            return false; // TLD only one letter.

        if (mailAddress.User.Contains(' '))
            return false;
        if (mailAddress.User.Split('.').Any(p => p == string.Empty))
            return false; // Double dot or dot at end of user part.

        return true;
    }
}
