using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MvcHybrid
{
    public class LogoutSessionManager
    {
        private static readonly Object _lock = new Object();
        private const string redisItemeKey = "logoutSessions";
        private readonly ILogger<LogoutSessionManager> _logger;
        private IDistributedCache _cache;
        private List<Session> _sessions = new List<Session>();

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
            _logger.LogWarning($"Add a logout to the session: sub: {sub}, sid: {sid}");
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(cacheExpirationInDays));

            lock (_lock)
            {
                var logoutSessions = _cache.GetString(redisItemeKey);
                if (logoutSessions != null)
                {
                    _sessions = JsonConvert.DeserializeObject<List<Session>>(logoutSessions);
                }

                if (!_sessions.Any(s => s.IsMatch(sub, sid)))
                {
                    _sessions.Add(new Session { Sub = sub, Sid = sid, Created = DateTime.UtcNow });
                }

                // remove all items which are older than an hour
                _sessions.RemoveAll(t => t.Created < DateTime.UtcNow.AddDays(-cacheExpirationInDays));

                _cache.SetString(redisItemeKey, JsonConvert.SerializeObject(_sessions), options);
            }
        }

        public async Task<bool> IsLoggedOutAsync(string sub, string sid)
        {
            var cdsLogoutSessions = await _cache.GetStringAsync(redisItemeKey);
            if (cdsLogoutSessions != null)
            {
                _sessions = JsonConvert.DeserializeObject<List<Session>>(cdsLogoutSessions);
            }

            if (_sessions == null)
            {
                _logger.LogWarning($"No Session");
                _sessions = new List<Session>();
            }

            var matches = _sessions.Any(s => s.IsMatch(sub, sid));
            _logger.LogWarning($"Logout session exists T/F {matches} : {sub}, sid: {sid}");
            return matches;
        }

        private class Session
        {
            public DateTime Created { get; set; }
            public string Sub { get; set; }
            public string Sid { get; set; }

            public bool IsMatch(string sub, string sid)
            {
                return (Sid == sid && Sub == sub) ||
                       (Sid == sid && Sub == null) ||
                       (Sid == null && Sub == sub);
            }
        }
    }
}
