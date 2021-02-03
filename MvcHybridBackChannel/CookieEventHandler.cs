using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace MvcHybrid
{
    public class CookieEventHandler : CookieAuthenticationEvents
    {
        private readonly LogoutSessionManager _logoutSessionManager;

        public CookieEventHandler(LogoutSessionManager logoutSessions)
        {
            _logoutSessionManager = logoutSessions;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            if (context.Principal.Identity.IsAuthenticated)
            {
                var sub = context.Principal.FindFirst("sub")?.Value;
                var sid = context.Principal.FindFirst("sid")?.Value;

                if (await _logoutSessionManager.IsLoggedOutAsync(sub, sid))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        }
    }
}