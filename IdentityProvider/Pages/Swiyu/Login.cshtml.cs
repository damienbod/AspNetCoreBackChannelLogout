using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Idp.Swiyu.Passkeys.Sts.Domain;
using Idp.Swiyu.Passkeys.Sts.Domain.Models;
using Idp.Swiyu.Passkeys.Sts.SwiyuServices;
using ImageMagick;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Net.Codecrete.QrCodeGenerator;
using System.Security.Claims;
using System.Text.Json;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Login;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IIdentityProviderStore _identityProviderStore;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ApplicationDbContext _applicationDbContext;

    [BindProperty]
    public string ReturnUrl { get; set; } = default!;

    private readonly VerificationService _verificationService;
    private readonly string? _swiyuOid4vpUrl;

    [BindProperty]
    public string? VerificationId { get; set; }

    [BindProperty]
    public string? QrCodeUrl { get; set; } = string.Empty;

    [BindProperty]
    public string? DeepLink { get; set; } = string.Empty;

    [BindProperty]
    public byte[]? QrCodePng { get; set; } = [];

    public LoginModel(
        IIdentityServerInteractionService interaction,
        IAuthenticationSchemeProvider schemeProvider,
        IIdentityProviderStore identityProviderStore,
        IEventService events,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        VerificationService verificationService,
        IHttpClientFactory clientFactory,
        IConfiguration configuration,
        ApplicationDbContext applicationDbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _interaction = interaction;
        _schemeProvider = schemeProvider;
        _identityProviderStore = identityProviderStore;
        _events = events;

        _clientFactory = clientFactory;
        _applicationDbContext = applicationDbContext;

        _verificationService = verificationService;
        _swiyuOid4vpUrl = configuration["SwiyuOid4vpUrl"];
        QrCodeUrl = QrCodeUrl.Replace("{OID4VP_URL}", _swiyuOid4vpUrl);
    }

    public async Task<IActionResult> OnGet(string? returnUrl)
    {
        if (returnUrl != null)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl, HttpContext.RequestAborted);

            ReturnUrl = returnUrl;
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

        return Page();
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

            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(ReturnUrl, HttpContext.RequestAborted);

            if (ModelState.IsValid)
            {
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

                    var additionalClaims = GetAdditionalClaims(exists);

                    if(User.IsAuthenticated())
                    {
                        if(user.Email != exists.Email)
                        {
                            return Unauthorized();
                        }
                    }
                    // issue authentication cookie for user
                    await _signInManager.SignInWithClaimsAsync(user, null, additionalClaims);

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            // The client is native, so this change in how to
                            // return the response is for better UX for the end user.
                            return this.LoadingPage(ReturnUrl);
                        }
                    }

                    return Redirect(ReturnUrl);
                }
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

    private static List<Claim> GetAdditionalClaims(SwiyuIdentity swiyuVerifiedIdentity)
    {
        var additionalClaims = new List<Claim>
        {
            new Claim(Consts.LOA, Consts.LOA_200),
            new Claim(Consts.LOI, Consts.LOI_400),
            new Claim(RFC8485.VOT, RFC8485.BuildVoTFromLoiLoaAmr(Consts.LOI_400, Consts.LOA_200, Amr.Mca)),
            // ASP.NET Core bug workaround:
            // https://github.com/dotnet/aspnetcore/issues/64881
            new Claim(JwtClaimTypes.AuthenticationMethod, Amr.Mca),

            new Claim(JwtClaimTypes.GivenName, swiyuVerifiedIdentity.GivenName),
            new Claim(JwtClaimTypes.FamilyName, swiyuVerifiedIdentity.FamilyName),
            new Claim(JwtClaimTypes.BirthDate, swiyuVerifiedIdentity.BirthDate),
            new Claim("birth_place", swiyuVerifiedIdentity.BirthPlace)
        };

        return additionalClaims;
    }

}
