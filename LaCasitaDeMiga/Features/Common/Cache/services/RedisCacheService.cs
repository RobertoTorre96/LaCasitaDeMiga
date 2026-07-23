using StackExchange.Redis;
using System.Text.Json;

namespace LaCasitaDeMiga.Features.Common.Cache.services {
    public class RedisCacheService: ICacheService {

        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) {
            try {
                var value = await _db.StringGetAsync(key);
                if (value.IsNullOrEmpty) return default;

                return JsonSerializer.Deserialize<T>(value!);
            } catch (Exception ex) {
                // Si Redis falla, no rompemos la app: seguimos como si no hubiera caché
                _logger.LogWarning(ex, "No se pudo leer la clave de caché {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) {
            try {
                var json = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, json, expiration);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "No se pudo guardar la clave de caché {Key}", key);
            }
        }

        public async Task RemoveAsync(string key) {
            try {
                await _db.KeyDeleteAsync(key);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "No se pudo eliminar la clave de caché {Key}", key);
            }
        }

        public async Task<long> GetVersionAsync(string versionKey) {
            var value = await _db.StringGetAsync(versionKey);
            return value.IsNullOrEmpty ? 0 : (long)value;
        }

        public async Task<long> IncrementVersionAsync(string versionKey) {
            // INCR es una operación atómica en Redis: segura ante escrituras concurrentes
            return await _db.StringIncrementAsync(versionKey);
        }
    }
}
