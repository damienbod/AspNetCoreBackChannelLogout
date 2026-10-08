using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MvcHybridBackChannelTwo.BackChannelLogout;

public class CookieEventHandler : CookieAuthenticationEvents
{
    private readonly LogoutSessionManager _logoutSessionManager;
    private readonly ILogger<CookieEventHandler> _logger;

    public CookieEventHandler(LogoutSessionManager logoutSessionManager, ILogger<CookieEventHandler> logger)
    {
        _logoutSessionManager = logoutSessionManager;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("BC ValidatePrincipal: {PrincipalIdentityIsAuthenticated}", context.Principal.Identity.IsAuthenticated);
            var sub = context.Principal.FindFirst("sub")?.Value;
            var sid = context.Principal.FindFirst("sid")?.Value;

            if (await _logoutSessionManager.IsLoggedOutAsync(sub, sid))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                await _logoutSessionManager.RemoveAsync(sub, sid);
            }
        }
    }
}