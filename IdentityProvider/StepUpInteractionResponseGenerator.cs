using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using System.Security.Claims;

namespace Idp.Swiyu.Passkeys.Sts;

public class StepUpInteractionResponseGenerator : AuthorizeInteractionResponseGenerator
{
    public StepUpInteractionResponseGenerator(
        IdentityServerOptions options,
        TimeProvider clock,
        ILogger<AuthorizeInteractionResponseGenerator> logger,
        IConsentService consent,
        IProfileService profile) : base(options, clock, logger, consent, profile)
    {
    }

    protected override async Task<InteractionResponse> ProcessLoginAsync(ValidatedAuthorizeRequest request, CancellationToken ct)
    {
        var result = await base.ProcessLoginAsync(request, ct);

        if (!result.IsLogin && !result.IsError)
        {
            if (PasskeysRequired(request) && !AuthenticatedWithPasskeys(request.Subject!))
            {
                if (UserDeclinedMfa(request.Subject!))
                {
                    result.Error = OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements;
                }
                else
                {
                    // passkeys can be completed here
                    result.RedirectUrl = "/Account/Login";
                }
            }
            else if (MfaRequired(request) && !AuthenticatedWithMfa(request.Subject!))
            {
                if (UserDeclinedMfa(request.Subject!))
                {
                    result.Error = OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements;
                }
                else
                {
                    // Swiyu authentication possible
                    result.RedirectUrl = "/Account/Login";

                    // if you support the default Identity setup with MFA,
                    //result.RedirectUrl = "/Account/LoginWith2fa";
                }
            }
        }

        return result;
    }

    private bool PasskeysRequired(ValidatedAuthorizeRequest request) =>
       PasskeysRequestedByClient(request);

    private bool PasskeysRequestedByClient(ValidatedAuthorizeRequest request)
    {
        return request.AuthenticationContextReferenceClasses!.Contains("phr");
    }

    private bool MfaRequired(ValidatedAuthorizeRequest request) =>
       MfaRequestedByClient(request);

    private bool MfaRequestedByClient(ValidatedAuthorizeRequest request)
    {
        return request.AuthenticationContextReferenceClasses!.Contains("mfa");
    }

    private bool AuthenticatedWithMfa(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "amr" && (c.Value == Amr.Pop || c.Value == Amr.Mfa));

    private bool AuthenticatedWithPasskeys(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "amr" && c.Value == Amr.Pop);

    private bool UserDeclinedMfa(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "declined_mfa" && c.Value == "true");
}