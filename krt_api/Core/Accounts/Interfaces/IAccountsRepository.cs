using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Interfaces;
using krt_api.Core.Utils.Enums;

namespace krt_api.Core.Accounts.Interfaces
{
    public interface IAccountsRepository : IRepository<Entities.Accounts>
    {
        Task<Entities.Accounts> GetByCPFAsync(string cpf);
        Task<bool> AnotherUserRegisterWithSameCPF(Guid id, string cpf);
        Task<Entities.Accounts> GetByIdAsync(Guid id);
        Task<ListAllAccountsResponseDto> GetAllAsync(string? filter = null, AccountStatus? status = null, OrderBy orderBy = OrderBy.Descending, int page = 1);
    }
}
