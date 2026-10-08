// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Home;

[AllowAnonymous]
public class Index : PageModel
{
    public string Version => typeof(Duende.IdentityServer.Hosting.IdentityServerMiddleware).Assembly
                                 .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                 ?.InformationalVersion.Split('+').First()
                             ?? "unavailable";
}
