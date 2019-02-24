using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace MvcHybrid.Controllers
{
    public class HomeController : Controller
    {
        private AuthConfiguration _optionsAuthConfiguration;
        private readonly IHttpClientFactory _clientFactory;
        private IConfiguration _configuration;

        public HomeController(
            IOptions<AuthConfiguration> optionsAuthConfiguration, 
            IConfiguration configuration,
            IHttpClientFactory clientFactory)
        {
            _configuration = configuration;
            _optionsAuthConfiguration = optionsAuthConfiguration.Value;
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {
            var cs = _configuration["Test"];
            return View("Index",  cs);
        }

        [Authorize]
        public IActionResult Secure()
        {
            return View();
        }

        public async Task<IActionResult> RenewTokens()
        {
            var tokenclient = _clientFactory.CreateClient();

            var disco = await HttpClientDiscoveryExtensions.GetDiscoveryDocumentAsync(
                tokenclient, 
                _optionsAuthConfiguration.StsServerIdentityUrl);

            if (disco.IsError)
            {
                throw new ApplicationException($"Status code: {disco.IsError}, Error: {disco.Error}");
            }

            //var tokenResponse = await HttpClientTokenRequestExtensions.RequestClientCredentialsTokenAsync(tokenclient, new ClientCredentialsTokenRequest
            //{
            //    Scope = "scope_used_for_api_in_protected_zone",
            //    ClientSecret = "api_in_protected_zone_secret",
            //    Address = disco.TokenEndpoint,
            //    ClientId = "ProtectedApi"
            //});



            var tokenClient = new TokenClient(disco.TokenEndpoint, "mvc.hybrid.backchannel", "secret");
            var rt = await HttpContext.GetTokenAsync("refresh_token");
            var tokenResult = await tokenClient.RequestRefreshTokenAsync(rt);

            if (!tokenResult.IsError)
            {
                var old_id_token = await HttpContext.GetTokenAsync("id_token");
                var new_access_token = tokenResult.AccessToken;
                var new_refresh_token = tokenResult.RefreshToken;

                var tokens = new List<AuthenticationToken>
                {
                    new AuthenticationToken { Name = OpenIdConnectParameterNames.IdToken, Value = old_id_token },
                    new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = new_access_token },
                    new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = new_refresh_token }
                };

                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
                tokens.Add(new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) });

                var info = await HttpContext.AuthenticateAsync("Cookies");
                info.Properties.StoreTokens(tokens);
                await HttpContext.SignInAsync("Cookies", info.Principal, info.Properties);

                return Redirect("~/Home/Secure");
            }

            ViewData["Error"] = tokenResult.Error;
            return View("Error");
        }

        public IActionResult Logout()
        {
            return new SignOutResult(new[] { "Cookies", "oidc" });
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
