using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Utils;
using krt_api.Core.Utils.Enums;

namespace krt_api.Core.Accounts.Interfaces
{
    public interface IAccountsService
    {
        Task<ResponseModel> CreateAsync(CreateAccountDto dto);
        Task<ResponseModel> UpdateAsync(UpdateAccountDto dto);
        Task<ResponseModel> GetAsync(Guid id);
        Task<ResponseModel> GetAllAsync(string? filter = null, AccountStatus? status = null, OrderBy orderBy = OrderBy.Descending, int page = 1);
        Task<ResponseModel> DeleteAsync(Guid id);
    }
}
