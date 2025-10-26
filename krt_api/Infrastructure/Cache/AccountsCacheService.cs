using krt_api.Core.Accounts.Entities;
using krt_api.Core.Accounts.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace krt_api.Infrastructure.Cache
{
    public class AccountCacheService : IAccountCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AccountCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SaveAccountAsync(Accounts data, TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(6)
            };

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await _cache.SetStringAsync(GetKey(data.Id), json, options);
        }
        public async Task<Accounts?> GetAccountAsync(Guid id)
        {
            var json = await _cache.GetStringAsync(GetKey(id));
            return json is null ? null : JsonSerializer.Deserialize<Accounts>(json, _jsonOptions);
        }
        public async Task RemoveAccountAsync(Guid id)
        {
            await _cache.RemoveAsync(GetKey(id));
        }
        private static string GetKey(Guid id) => $"account:{id}";
    }
}
