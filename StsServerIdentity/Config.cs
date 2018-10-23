// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace StsServerIdentity
{
    public class Config
    {
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
        public static IEnumerable<Client> GetClients(IConfigurationSection stsConfig)
        {
            var mvcHybridBackchannelClientTwoUrl = stsConfig["MvcHybridBackchannelClientTwoUrl"];
            var mvcHybridBackchannelClientUrl = stsConfig["MvcHybridBackchannelClientUrl"];
            // TODO use configs in app

            // client credentials client
            return new List<Client>
            {
                new Client
                {
                    ClientId = "mvc.hybrid.backchannel",
                    ClientName = "MVC Hybrid (with BackChannel logout)",
                    ClientUri = "http://identityserver.io",

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.Hybrid,
                    AllowAccessTokensViaBrowser = false,

                    RedirectUris = { $"{mvcHybridBackchannelClientUrl}/signin-oidc" },
                    BackChannelLogoutSessionRequired = true,
                    BackChannelLogoutUri = $"{mvcHybridBackchannelClientUrl}/logout",
                    PostLogoutRedirectUris = { $"{mvcHybridBackchannelClientUrl}/signout-callback-oidc" },

                    AllowOfflineAccess = true,

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email
                    }
                },
                new Client
                {
                    ClientId = "mvc.hybrid.backchanneltwo",
                    ClientName = "MVC Hybrid (with BackChannel logout two)",
                    ClientUri = "http://identityserver.io",

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.Hybrid,
                    AllowAccessTokensViaBrowser = false,

                    RedirectUris = { $"{mvcHybridBackchannelClientTwoUrl}/signin-oidc" },
                    BackChannelLogoutUri = $"{mvcHybridBackchannelClientTwoUrl}/logout",
                    BackChannelLogoutSessionRequired = true,
                    PostLogoutRedirectUris = { $"{mvcHybridBackchannelClientTwoUrl}/signout-callback-oidc" },

                    AllowOfflineAccess = true,

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email
                    }
                }
            };
        }
    }
}