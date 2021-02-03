﻿using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MvcHybridBackChannelTwo.BackChannelLogout;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcHybridBackChannelTwo.Controllers
{
    public class LogoutController : Controller
    {
        public LogoutSessionManager _logoutSessionsManager { get; }
        private AuthConfiguration _optionsAuthConfiguration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<LogoutController> _logger;

        public LogoutController(
            LogoutSessionManager logoutSessions,
            IOptions<AuthConfiguration> optionsAuthConfiguration,
            ILogger<LogoutController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _optionsAuthConfiguration = optionsAuthConfiguration.Value;
            _logoutSessionsManager = logoutSessions;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string logout_token)
        {
            _logger.LogInformation($"BC Logout event from server: {logout_token}");

            // MvcHybridBackChannelTwoBackChannelTwo Backchannel Logout from the server
            Response.Headers.Add("Cache-Control", "no-cache, no-store");
            Response.Headers.Add("Pragma", "no-cache");

            try
            {
                var user = await ValidateLogoutToken(logout_token);

                // these are the sub & sid to signout
                var sub = user.FindFirst("sub")?.Value;
                var sid = user.FindFirst("sid")?.Value;

                _logoutSessionsManager.Add(sub, sid);

                //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message}");
            }
            return BadRequest();
        }

        private async Task<ClaimsPrincipal> ValidateLogoutToken(string logoutToken)
        {
            var claims = await ValidateJwt(logoutToken);

            if (claims.FindFirst("sub") == null && claims.FindFirst("sid") == null)
            {
                throw new Exception("BC Invalid logout token sub or sid is missing");
            }

            var nonce = claims.FindFirstValue("nonce");
            if (!string.IsNullOrWhiteSpace(nonce))
            {
                throw new Exception("BC Invalid logout token, no nonce");
            }

            var eventsJson = claims.FindFirst("events")?.Value;
            if (string.IsNullOrWhiteSpace(eventsJson))
            {
                throw new Exception("BC Invalid logout token, missing events");
            }

            var events = JObject.Parse(eventsJson);
            JToken logoutTokenData;
            var logoutEvent = events.TryGetValue("http://schemas.openid.net/event/backchannel-logout", out logoutTokenData);
            if (logoutEvent == false)
            {
                _logger.LogInformation($"BC Invalid logout token {logoutTokenData}");
                // 2.6 Logout Token Validation
                throw new Exception("BC Invalid logout token");
            }

            return claims;
        }

        private async Task<ClaimsPrincipal> ValidateJwt(string jwt)
        {
            var disco = await HttpClientDiscoveryExtensions.GetDiscoveryDocumentAsync(
               _httpClient, _optionsAuthConfiguration.StsServerIdentityUrl);

            var keys = new List<SecurityKey>();
            foreach (var webKey in disco.KeySet.Keys)
            {
                var key = new JsonWebKey()
                {
                    Kty = webKey.Kty,
                    Alg = webKey.Alg,
                    Kid = webKey.Kid,
                    X = webKey.X,
                    Y = webKey.Y,
                    Crv = webKey.Crv,
                    E = webKey.E,
                    N = webKey.N,
                };
                keys.Add(key);
            }

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = disco.Issuer,
                ValidAudience = _optionsAuthConfiguration.Audience,
                IssuerSigningKeys = keys,

                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role
            };

            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();

            var user = handler.ValidateToken(jwt, parameters, out var _);
            return user;
        }
    }
}
