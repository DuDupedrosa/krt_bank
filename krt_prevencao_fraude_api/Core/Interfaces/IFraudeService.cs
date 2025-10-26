using krt_prevencao_fraude_api.Core.Dtos;

namespace krt_prevencao_fraude_api.Core.Interfaces
{
    public interface IFraudeService
    {
        Task OnCreateAccountAsync(AccountDto dto);
        Task OnDeleteAccountAsync(AccountDto dto);
        Task OnUpdateAccountAsync(AccountDto dto);
    }
}
