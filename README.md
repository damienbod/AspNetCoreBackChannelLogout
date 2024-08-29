# ASP.NET Core OpenID Connect Back-Channel Logout for Hybrid Clients

[![.NET](https://github.com/damienbod/AspNetCoreBackChannelLogout/actions/workflows/dotnet.yml/badge.svg)](https://github.com/damienbod/AspNetCoreBackChannelLogout/actions/workflows/dotnet.yml)

## Blogs

- [OpenID Connect back-channel logout using Azure Redis Cache and IdentityServer4](https://damienbod.com/2018/12/18/openid-connect-back-channel-logout-using-azure-redis-cache-and-identityserver4/)
- [Using Azure Key Vault with ASP.NET Core and Azure App Services](https://damienbod.com/2018/12/23/using-azure-key-vault-with-asp-net-core-and-azure-app-services/)
- [Deploying ASP.NET Core App Services using Azure Key Vault and Azure Resource Manager templates](https://damienbod.com/2019/01/07/deploying-asp-net-core-app-services-using-azure-key-vault-and-azure-resource-manager-templates/)
- [Using Azure Key Vault from a non-Azure App](https://damienbod.com/2019/02/07/using-azure-key-vault-from-an-non-azure-app/)

## Database Setup

The Secure Token Service is setup using Duende IdentityServer with Identity and Microsoft SQL Server. Change the connection string and initialize the database using EF Core migrations. 

```
dotnet ef migrations add initSts -c ApplicationDbContext

dotnet ef migrations add initPersistedGrant -c PersistedGrantDbContext

dotnet ef database update -c ApplicationDbContext

dotnet ef database update -c PersistedGrantDbContext
```

## History

- 2024-08-29 Updated .NET 8, Duende IDP
- 2021-02-02 Updated .NET 5, IdentityServer4
- 2019-02-24 Updated npm packages, removing obsolete APIs
- 2019-02-07 Added Standalone application example using Azure Key Vault
- 2018-12-26 Adding ARM template to set key vault secrets
- 2018-12-23 Adding Key Vault to the MvcHybridBackChannel project
- 2018-12-18 Added Azure Redis Cache, support for multi instance OIDC backchannel logout
- 2018-11-22 Updated, Nuget packages, npm packages, Logout controller

## Links

https://openid.net/specs/openid-connect-backchannel-1_0.html

https://github.com/DuendeSoftware/Samples/tree/main/IdentityServer/v7/SessionManagement

https://github.com/DuendeSoftware/Samples/tree/main/IdentityServer/v6/SessionManagement

https://github.com/DuendeSoftware/Samples/tree/main/IdentityServer/v5/Basics/MvcBackChannelLogout

http://docs.identityserver.io/en/release/topics/signout.html

https://medium.com/@robert.broeckelmann/openid-connect-logout-eccc73df758f

https://medium.com/@piraveenaparalogarajah/openid-connect-back-channel-logout-1-0-fe1f90c83fe5

https://ldapwiki.com/wiki/OpenID%20Connect%20Back-Channel%20Logout

https://datatracker.ietf.org/meeting/97/materials/slides-97-secevent-oidc-logout-01
