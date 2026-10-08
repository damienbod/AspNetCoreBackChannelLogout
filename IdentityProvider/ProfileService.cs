using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using System.Security.Claims;

namespace Idp.Swiyu.Passkeys.Sts;

public class ProfileService : IProfileService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProfileService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken ct)
    {
        // context.Subject is the user for whom the result is being made
        // context.Subject.Claims is the claims collection from the user's session cookie at login time
        // context.IssuedClaims is the collection of claims that your logic has decided to return in the response

        if (context.Caller == IdentityServerConstants.ProfileDataCallers.ClaimsProviderAccessToken)
        {
            // Access token - add custom claims
            AddCustomClaims(context);
        }

        if (context.Caller == IdentityServerConstants.ProfileDataCallers.ClaimsProviderIdentityToken)
        {
            // Identity token - add custom claims and standard profile claims
            AddCustomClaims(context);
            AddProfileClaims(context);
        }

        if (context.Caller == IdentityServerConstants.ProfileDataCallers.UserInfoEndpoint)
        {
            // UserInfo endpoint - add custom claims and standard profile claims
            AddCustomClaims(context);
            AddProfileClaims(context);
        }

        return Task.CompletedTask;
    }

    public Task IsActiveAsync(IsActiveContext context, CancellationToken ct)
    {
        context.IsActive = true;
        return Task.CompletedTask;
    }

    private void AddCustomClaims(ProfileDataRequestContext context)
    {
        // Add OID claim
        var oid = context.Subject.Claims.FirstOrDefault(t => t.Type == "oid");
        if (oid != null)
        {
            context.IssuedClaims.Add(new Claim("oid", oid.Value));
        }

        // Add LOA (Level of Authentication) claim
        var loa = context.Subject.Claims.FirstOrDefault(t => t.Type == Consts.LOA);
        if (loa != null)
        {
            context.IssuedClaims.Add(new Claim(Consts.LOA, loa.Value));
        }

        // Add LOI (Level of Identification) claim
        var loi = context.Subject.Claims.FirstOrDefault(t => t.Type == Consts.LOI);
        if (loi != null)
        {
            context.IssuedClaims.Add(new Claim(Consts.LOI, loi.Value));
        }

        // Add AMR (Authentication Method Reference) claim
        var amr = context.Subject.Claims.FirstOrDefault(t => t.Type == JwtClaimTypes.AuthenticationMethod);
        if (amr != null)
        {
            context.IssuedClaims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, amr.Value));
        }

        // Add LOI (Level of Identification) claim
        var vot = context.Subject.Claims.FirstOrDefault(t => t.Type == RFC8485.VOT);
        if (vot != null)
        {
            context.IssuedClaims.Add(new Claim(RFC8485.VOT, vot.Value));
            context.IssuedClaims.Add(new Claim("vtm", $"{GetCurrentAbsoluteUrl()}/vot.json"));
        }
    }

    private void AddProfileClaims(ProfileDataRequestContext context)
    {
        // Add Name claim (required for User.Identity.Name to work)
        var name = context.Subject.Claims.FirstOrDefault(t => t.Type == JwtClaimTypes.Name);
        if (name != null)
        {
            context.IssuedClaims.Add(new Claim(JwtClaimTypes.Name, name.Value));
        }

        var givenName = context.Subject.Claims.FirstOrDefault(t => t.Type == JwtClaimTypes.GivenName);
        if (givenName != null)
        {
            context.IssuedClaims.Add(new Claim(JwtClaimTypes.GivenName, givenName.Value));
        }

        var familyName = context.Subject.Claims.FirstOrDefault(t => t.Type == JwtClaimTypes.FamilyName);
        if (familyName != null)
        {
            context.IssuedClaims.Add(new Claim(JwtClaimTypes.FamilyName, familyName.Value));
        }

        var email = context.Subject.Claims.FirstOrDefault(t => t.Type == JwtClaimTypes.Email);
        if (email != null)
        {
            context.IssuedClaims.Add(new Claim(JwtClaimTypes.Email, email.Value));
        }
    }


    private string? GetCurrentAbsoluteUrl()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null; // no request context (e.g., background task)

        var req = ctx.Request;
        return $"{req.Scheme}://{req.Host}{req.PathBase}";
    }

}