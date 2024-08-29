using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace MvcHybridBackChannel.BackChannelLogout;

public partial class LogoutSessionManager
{
    private static readonly Object _lock = new Object();
    private readonly ILogger<LogoutSessionManager> _logger;
    private IDistributedCache _cache;

    // Amount of time to check for old sessions. If this is to long, the cache will increase, 
    // or if you have many user sessions, this will increase to much.
    private const int cacheExpirationInDays = 8;

    public LogoutSessionManager(ILoggerFactory loggerFactory, IDistributedCache cache)
    {
        _cache = cache;
        _logger = loggerFactory.CreateLogger<LogoutSessionManager>();
    }

    public void Add(string sub, string sid)
    {
        _logger.LogWarning($"BC Add a logout to the session: sub: {sub}, sid: {sid}");
        var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(cacheExpirationInDays));

        lock (_lock)
        {
            var key = sub + sid;
            var logoutSession = _cache.GetString(key);
            _logger.LogInformation($"BC logoutSession: {logoutSession}");
            if (logoutSession != null)
            {
                var session = JsonConvert.DeserializeObject<BackchannelLogoutSession>(logoutSession);
            }
            else
            {
                var newSession = new BackchannelLogoutSession { Sub = sub, Sid = sid };
                _cache.SetString(key, JsonConvert.SerializeObject(newSession), options);
            }
        }
    }

    public async Task<bool> IsLoggedOutAsync(string sub, string sid)
    {
        _logger.LogInformation($"BC IsLoggedOutAsync: sub: {sub}, sid: {sid}");
        var key = sub + sid;
        var matches = false;
        var logoutSession = await _cache.GetStringAsync(key);
        if (logoutSession != null)
        {
            var session = JsonConvert.DeserializeObject<BackchannelLogoutSession>(logoutSession);
            matches = session.IsMatch(sub, sid);
            _logger.LogInformation($"BC Logout session exists T/F {matches} : {sub}, sid: {sid}");
        }

        return matches;
    }
}