namespace LaCasitaDeMiga.Features.Common.Cache.services {
    public interface ICacheService {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
        Task RemoveAsync(string key);
        Task<long> GetVersionAsync(string versionKey);
        Task<long> IncrementVersionAsync(string versionKey);
    }
}
