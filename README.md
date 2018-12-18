# ASP.NET Core OpenID Connect Back-Channel Logout for Hybrid Clients

## Original code

The IdentityServer4 samples was used to build this example. The code was updated, but not changed. See this following link for the original:

https://github.com/IdentityServer/IdentityServer4.Samples/tree/release/Clients/src/MvcHybridBackChannel

## Database Setup

The Secure Token Service is setup using IdentityServer4 with Identity and Microsoft SQL Server. Change the connection string and initialize the database using EF Core migrations. 

```
dotnet ef migrations add initSts -c ApplicationDbContext

dotnet ef migrations add initPersistedGrant -c PersistedGrantDbContext

dotnet ef database update -c ApplicationDbContext

dotnet ef database update -c PersistedGrantDbContext
```

## History

2018-12-18 Added Azure Redis Cache, support for multi instance OIDC backchannel logout

2018-11-22 Updated, Nuget packages, npm packages, Logout controller

## Links

https://openid.net/specs/openid-connect-backchannel-1_0.html

http://docs.identityserver.io/en/release/topics/signout.html

https://medium.com/@robert.broeckelmann/openid-connect-logout-eccc73df758f

https://medium.com/@piraveenaparalogarajah/openid-connect-back-channel-logout-1-0-fe1f90c83fe5

https://ldapwiki.com/wiki/OpenID%20Connect%20Back-Channel%20Logout

https://datatracker.ietf.org/meeting/97/materials/slides-97-secevent-oidc-logout-01