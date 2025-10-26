using krt_cartoes_api.Core.Dtos;

namespace krt_cartoes_api.Core.Interfaces
{
    public interface ICartoesService
    {
        Task OnCreateAccountAsync(AccountDto dto);
        Task OnDeleteAccountAsync(AccountDto dto);
        Task OnUpdateAccountAsync(AccountDto dto);
    }
}
