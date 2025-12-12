// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace StsServerIdentity;

public class Config
{
    public static IEnumerable<ApiScope> GetApiScopes()
    {
        return new List<ApiScope>
        {
            new ApiScope("scope_used_for_hybrid_flow", "Scope for the scope_used_for_hybrid_flow"),
            new ApiScope("scope_used_for_api_in_protected_zone",  "Scope for the scope_used_for_api_in_protected_zone")
        };
    }

    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
        return new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };
    }

    public static IEnumerable<ApiResource> GetApiResources()
    {
        return new List<ApiResource>();
    }

    // clients want to access resources (aka scopes)
    public static IEnumerable<Client> GetClients(IConfiguration stsConfig)
    {
        var mvcHybridBackchannelClientTwoUrl = stsConfig["MvcHybridBackchannelClientTwoUrl"];
        var mvcHybridBackchannelClientUrl = stsConfig["MvcHybridBackchannelClientUrl"];

        return new List<Client>
        {
            new Client
            {
                ClientId = "mvc.hybrid.backchannel",
                ClientName = "MVC Hybrid (with BackChannel logout)",
                ClientSecrets = {new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = true,
                AllowOfflineAccess = true,
                AlwaysSendClientClaims = true,
                UpdateAccessTokenClaimsOnRefresh = true,

                RedirectUris = { $"{mvcHybridBackchannelClientUrl}/signin-oidc" },
                BackChannelLogoutSessionRequired = true,
                BackChannelLogoutUri = $"{mvcHybridBackchannelClientUrl}/logout",
                PostLogoutRedirectUris = { $"{mvcHybridBackchannelClientUrl}/signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    IdentityServerConstants.StandardScopes.Email
                }
            },
            new Client
            {
                ClientId = "mvc.hybrid.backchanneltwo",
                ClientName = "MVC Hybrid (with BackChannel logout two)",
                ClientSecrets = {new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = true,
                AllowOfflineAccess = true,
                AlwaysSendClientClaims = true,
                UpdateAccessTokenClaimsOnRefresh = true,

                RedirectUris = { $"{mvcHybridBackchannelClientTwoUrl}/signin-oidc" },
                BackChannelLogoutUri = $"{mvcHybridBackchannelClientTwoUrl}/logout",
                BackChannelLogoutSessionRequired = true,
                PostLogoutRedirectUris = { $"{mvcHybridBackchannelClientTwoUrl}/signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    IdentityServerConstants.StandardScopes.Email
                }
            }
        };
    }
}