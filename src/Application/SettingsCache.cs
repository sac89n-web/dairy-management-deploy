using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Dairy.Application
{
    public class SettingsCache
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly string _connStr;
        public SettingsCache(IMemoryCache cache, IConfiguration config)
        {
            _cache = cache;
            _config = config;
            _connStr = config.GetConnectionString("Postgres")!;
        }
        public async Task<(string SystemName, string Contact, string Address)> GetSettingsAsync()
        {
            if (_cache.TryGetValue("settings", out (string, string, string) settings))
                return settings;
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT system_name, contact, address FROM dairy.settings LIMIT 1", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                settings = (reader.GetString(0), reader.GetString(1), reader.GetString(2));
                _cache.Set("settings", settings, TimeSpan.FromMinutes(10));
                return settings;
            }
            return ("", "", "");
        }
        public void Invalidate() => _cache.Remove("settings");
    }
}
