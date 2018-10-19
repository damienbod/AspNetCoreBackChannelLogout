# AspNetCoreBackChannelLogout

dotnet ef migrations add initSts -c ApplicationDbContext

dotnet ef migrations add initPersistedGrant -c PersistedGrantDbContext

dotnet ef database update -c ApplicationDbContext

dotnet ef database update -c PersistedGrantDbContext