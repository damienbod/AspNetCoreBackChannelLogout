// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Idp.Swiyu.Passkeys.Sts.Domain;
using Idp.Swiyu.Passkeys.Sts.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Login;

[SecurityHeaders]
[AllowAnonymous]
public class Index : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IIdentityProviderStore _identityProviderStore;
    private readonly ApplicationDbContext _applicationDbContext;

    public ViewModel View { get; set; } = default!;

    [BindProperty] public InputModel Input { get; set; } = default!;

    public Index(
        IIdentityServerInteractionService interaction,
        IAuthenticationSchemeProvider schemeProvider,
        IIdentityProviderStore identityProviderStore,
        IEventService events,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext applicationDbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _interaction = interaction;
        _schemeProvider = schemeProvider;
        _identityProviderStore = identityProviderStore;
        _events = events;
        _applicationDbContext = applicationDbContext;
    }

    public async Task<IActionResult> OnGet(string? returnUrl)
    {
        await BuildModelAsync(returnUrl);

        if (View.IsExternalLoginOnly)
        {
            // we only have one option for logging in and it's an external provider
            return RedirectToPage("/ExternalLogin/Challenge", new { scheme = View.ExternalLoginScheme, returnUrl });
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        // check if we are in the context of an authorization request
        var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl, HttpContext.RequestAborted);

        var passkeySubmit = Input.Passkey != null && Input.Passkey.CredentialJson != null;

        // the user clicked the "cancel" button
        if (Input.Button != "login" && !passkeySubmit)
        {
            if (context != null)
            {
                // This "can't happen", because if the ReturnUrl was null, then the context would be null
                ArgumentNullException.ThrowIfNull(Input.ReturnUrl, nameof(Input.ReturnUrl));

                // if the user cancels, send a result back into IdentityServer as if they
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await _interaction.DenyAuthorizationAsync(context, InteractionError.AccessDenied, HttpContext.RequestAborted);

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage(Input.ReturnUrl);
                }

                return Redirect(Input.ReturnUrl ?? "~/");
            }
            else
            {
                // since we don't have a valid context, then we just go back to the home page
                return Redirect("~/");
            }
        }

        SignInResult? result = null;
        ApplicationUser? user = null;
        if (!string.IsNullOrEmpty(Input.Passkey?.CredentialJson))
        {
            // When performing passkey sign-in, don't perform form validation.
            ModelState.Clear();

            result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
            if (result.Succeeded)
            {
                user = await _userManager.GetUserAsync(User);
                var swiyuVerifiedIdentity = _applicationDbContext.SwiyuIdentity.FirstOrDefault(si => si.UserId == user!.Id);

                // Sign out first to clear the existing cookie
                await _signInManager.SignOutAsync();
                var additionalClaims = GetAdditionalClaims(swiyuVerifiedIdentity, Consts.LOA_400, Amr.Pop);

                // Sign in again with the additional claims
                await _signInManager.SignInWithClaimsAsync(user!, isPersistent: false, additionalClaims);
            }
        }
        else if (ModelState.IsValid)
        {
            // Only remember login if allowed
            var rememberLogin = LoginOptions.AllowRememberLogin && Input.RememberLogin;

            result = await _signInManager.PasswordSignInAsync(Input.Username!, Input.Password!, isPersistent: rememberLogin, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                user = await _userManager.FindByNameAsync(Input.Username!);
                var swiyuVerifiedIdentity = _applicationDbContext.SwiyuIdentity.FirstOrDefault(si => si.UserId == user!.Id);

                // Sign out first to clear the existing cookie
                await _signInManager.SignOutAsync();

                // Create additional claims
                var additionalClaims = GetAdditionalClaims(swiyuVerifiedIdentity, Consts.LOA_100, Amr.Pwd);

                // Sign in again with the additional claims
                await _signInManager.SignInWithClaimsAsync(user!, isPersistent: false, additionalClaims);
            }
        }

        if (result?.Succeeded == true && user != null)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(user!.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId), HttpContext.RequestAborted);
            Telemetry.Metrics.UserLogin(context?.Client.ClientId, IdentityServerConstants.LocalIdentityProvider);

            if (context != null)
            {
                // This "can't happen", because if the ReturnUrl was null, then the context would be null
                ArgumentNullException.ThrowIfNull(Input.ReturnUrl, nameof(Input.ReturnUrl));

                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage(Input.ReturnUrl);
                }

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                return Redirect(Input.ReturnUrl ?? "~/");
            }

            // request for a local page
            if (Url.IsLocalUrl(Input.ReturnUrl))
            {
                return Redirect(Input.ReturnUrl);
            }
            else if (string.IsNullOrEmpty(Input.ReturnUrl))
            {
                return Redirect("~/");
            }
            else
            {
                // user might have clicked on a malicious link - should be logged
                throw new ArgumentException("invalid return URL");
            }
        }

        const string error = "invalid credentials";
        await _events.RaiseAsync(new UserLoginFailureEvent(Input.Username, error, clientId: context?.Client.ClientId), HttpContext.RequestAborted);
        Telemetry.Metrics.UserLoginFailure(context?.Client.ClientId, IdentityServerConstants.LocalIdentityProvider, error);
        ModelState.AddModelError(string.Empty, LoginOptions.InvalidCredentialsErrorMessage);

        // something went wrong, show form with error
        await BuildModelAsync(Input.ReturnUrl);
        return Page();
    }

    private static List<Claim> GetAdditionalClaims(SwiyuIdentity? swiyuVerifiedIdentity, string loaValue, string amr)
    {
        List<Claim> additionalClaims;
        if (swiyuVerifiedIdentity != null)
        {
            additionalClaims = new List<Claim>
            {
                new Claim(Consts.LOA, loaValue),
                new Claim(Consts.LOI, Consts.LOI_400),
                new Claim(RFC8485.VOT, RFC8485.BuildVoTFromLoiLoaAmr(Consts.LOI_400, loaValue, amr)),
                // ASP.NET Core bug workaround:
                // https://github.com/dotnet/aspnetcore/issues/64881
                new Claim(JwtClaimTypes.AuthenticationMethod, amr),

                new Claim(JwtClaimTypes.GivenName, swiyuVerifiedIdentity.GivenName),
                new Claim(JwtClaimTypes.FamilyName, swiyuVerifiedIdentity.FamilyName),
                new Claim(JwtClaimTypes.BirthDate, swiyuVerifiedIdentity.BirthDate),
                new Claim("birth_place", swiyuVerifiedIdentity.BirthPlace)
            };
        }
        else
        {
            additionalClaims = new List<Claim>
            {
                new Claim(Consts.LOA, loaValue),
                new Claim(Consts.LOI, Consts.LOI_100),
                new Claim(RFC8485.VOT, RFC8485.BuildVoTFromLoiLoaAmr(Consts.LOI_100, loaValue, amr)),
                // ASP.NET Core bug workaround:
                // https://github.com/dotnet/aspnetcore/issues/64881
                new Claim(JwtClaimTypes.AuthenticationMethod, amr)
            };
        }

        return additionalClaims;
    }

    private async Task BuildModelAsync(string? returnUrl)
    {
        Input = new InputModel
        {
            ReturnUrl = returnUrl
        };

        var context = await _interaction.GetAuthorizationContextAsync(returnUrl, HttpContext.RequestAborted);
        if (context?.IdP != null)
        {
            var scheme = await _schemeProvider.GetSchemeAsync(context.IdP);
            if (scheme != null)
            {
                var local = context.IdP == Duende.IdentityServer.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                View = new ViewModel
                {
                    EnableLocalLogin = local,
                };

                Input.Username = context.LoginHint;

                if (!local)
                {
                    View.ExternalProviders =
                    [
                        new ViewModel.ExternalProvider(authenticationScheme: context.IdP,
                            displayName: scheme.DisplayName)
                    ];
                }
            }

            return;
        }

        var schemes = await _schemeProvider.GetAllSchemesAsync();

        var providers = schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ViewModel.ExternalProvider
            (
                authenticationScheme: x.Name,
                displayName: x.DisplayName ?? x.Name
            )).ToList();

        var dynamicSchemes = (await _identityProviderStore.GetAllSchemeNamesAsync(HttpContext.RequestAborted))
            .Where(x => x.Enabled)
            .Select(x => new ViewModel.ExternalProvider
            (
                authenticationScheme: x.Scheme,
                displayName: x.DisplayName ?? x.Scheme
            ));
        providers.AddRange(dynamicSchemes);


        var allowLocal = true;
        var client = context?.Client;
        if (client != null)
        {
            allowLocal = client.EnableLocalLogin;
            if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Count != 0)
            {
                providers = providers.Where(provider =>
                    client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
            }
        }

        View = new ViewModel
        {
            AllowRememberLogin = LoginOptions.AllowRememberLogin,
            EnableLocalLogin = allowLocal && LoginOptions.AllowLocalLogin,
            ExternalProviders = providers.ToArray()
        };
    }
}
