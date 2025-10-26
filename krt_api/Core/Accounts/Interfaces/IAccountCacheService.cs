namespace krt_api.Core.Accounts.Interfaces
{
    public interface IAccountCacheService
    {
        Task SaveAccountAsync(Entities.Accounts data, TimeSpan? expiration = null);
        Task<Entities.Accounts?> GetAccountAsync(Guid id);
        Task RemoveAccountAsync(Guid id);
    }
}
