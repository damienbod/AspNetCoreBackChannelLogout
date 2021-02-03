﻿using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace MvcHybridBackChannelTwo.BackChannelLogout
{
    public class CookieEventHandler : CookieAuthenticationEvents
    {
        private readonly LogoutSessionManager _logoutSessionManager;
        private readonly ILogger<CookieEventHandler> _logger;

        public CookieEventHandler(LogoutSessionManager logoutSessions, ILoggerFactory loggerFactory)
        {
            _logoutSessionManager = logoutSessions;
            _logger = loggerFactory.CreateLogger<CookieEventHandler>();
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            if (context.Principal.Identity.IsAuthenticated)
            {
                _logger.LogDebug($"ValidatePrincipal: {context.Principal.Identity.IsAuthenticated}");
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