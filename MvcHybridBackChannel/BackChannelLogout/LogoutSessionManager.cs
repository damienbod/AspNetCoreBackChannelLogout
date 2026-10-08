using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MvcHybridBackChannel.BackChannelLogout;

public class LogoutSessionManager
{
    private readonly ILogger<LogoutSessionManager> _logger;
    private readonly IDistributedCache _cache;

    // Amount of time to check for old sessions. If this is to long, the cache will increase, 
    // or if you have many user sessions, this will increase to much.
    // If this is too big/long or if you have many users with logout sessions, the cache size will increase
    private const int cacheExpirationInDays = 8;

    public LogoutSessionManager(ILogger<LogoutSessionManager> logger, IDistributedCache cache)
    {
        _cache = cache;
        _logger = logger;
    }

    public void Add(string? sub, string? sid)
    {
        _logger.LogInformation("BC Add logout session to cache. sub: '{Sub}', sid: '{Sid}'", sub, sid);

        var options = new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromDays(cacheExpirationInDays));

        var key = GetCacheKey(sub, sid);
        var logoutSession = _cache.GetString(key);


        if (logoutSession != null)
        {
            var session = JsonSerializer.Deserialize<BackchannelLogoutSession>(logoutSession);
            _logger.LogInformation("BC existing logoutSession: {LogoutSession}", logoutSession);
        }
        else
        {
            var newSession = new BackchannelLogoutSession { Sub = sub, Sid = sid };
            _cache.SetString(key, JsonSerializer.Serialize(newSession), options);
            _logger.LogInformation("BC created new logoutSession: {Key}", key);
        }
    }

    public async Task<bool> IsLoggedOutAsync(string? sub, string? sid)
    {
        _logger.LogInformation("BC IsLoggedOutAsync: sub: {Sub}, sid: {Sid}", sub, sid);
        var key = GetCacheKey(sub, sid);

        var isLoggedOut = false;
        var logoutSession = await _cache.GetStringAsync(key);
        if (logoutSession != null)
        {
            var session = JsonSerializer.Deserialize<BackchannelLogoutSession>(logoutSession);
            if (session != null)
            {
                isLoggedOut = session.IsMatch(sub, sid);
            }

            _logger.LogInformation("BC Logout session exists T/F {IsLoggedOut} : {Sub}, sid: {Sid}", isLoggedOut, sub, sid);
        }

        return isLoggedOut;
    }

    public async Task RemoveAsync(string? sub, string? sid)
    {
        _logger.LogInformation("BC Remove logout session from cache. sub: '{Sub}', sid: '{Sid}'", sub, sid);
        var key = GetCacheKey(sub, sid);
        await _cache.RemoveAsync(key);
    }

    private static string GetCacheKey(string? sub, string? sid)
    {
        return $"{sub}{sid}";
    }
}