using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MvcHybrid.Controllers
{
    public class LogoutController : Controller
    {
        public LogoutSessionManager _logoutSessionsManager { get; }
        private AuthConfiguration _optionsAuthConfiguration;
        private readonly HttpClient _httpClient;

        public LogoutController(
            LogoutSessionManager logoutSessions,
            IOptions<AuthConfiguration> optionsAuthConfiguration,
            IHttpClientFactory httpClientFactory)
        {
            _optionsAuthConfiguration = optionsAuthConfiguration.Value;
            _logoutSessionsManager = logoutSessions;
            _httpClient = httpClientFactory.CreateClient();
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string logout_token)
        {
            // MvcHybridBackChannel Backchannel Logout from the server
            Response.Headers.Add("Cache-Control", "no-cache, no-store");
            Response.Headers.Add("Pragma", "no-cache");

            try
            {
                var user = await ValidateLogoutToken(logout_token);

                // these are the sub & sid to signout
                var sub = user.FindFirst("sub")?.Value;
                var sid = user.FindFirst("sid")?.Value;

                _logoutSessionsManager.Add(sub, sid);

                return Ok();
            }
            catch { }

            return BadRequest();
        }


        private async Task<ClaimsPrincipal> ValidateLogoutToken(string logoutToken)
        {
            var claims = await ValidateJwt(logoutToken);

            if (claims.FindFirst("sub") == null && claims.FindFirst("sid") == null) throw new Exception("Invalid logout token");

            var nonce = claims.FindFirstValue("nonce");
            if (!String.IsNullOrWhiteSpace(nonce)) throw new Exception("Invalid logout token");

            var eventsJson = claims.FindFirst("events")?.Value;
            if (String.IsNullOrWhiteSpace(eventsJson)) throw new Exception("Invalid logout token");

            var events = JObject.Parse(eventsJson);
            var logoutEvent = events.TryGetValue("http://schemas.openid.net/event/backchannel-logout", out _);
            if (logoutEvent == false)
            {
                // 2.6 Logout Token Validation
                throw new Exception("Invalid logout token");
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
