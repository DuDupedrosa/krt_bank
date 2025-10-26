using krt_prevencao_fraude_api.Core.Dtos;
using krt_prevencao_fraude_api.Core.Interfaces;

namespace krt_prevencao_fraude_api.Application.Services
{
    // serviço responsável por lidar com a lógica de prevenção a fraude
    // como é só um exemplo para ser utilizado como consumer, a lógica será apenas um Console.WriteLine
    // mas está totalmente funcional, assim que o produtor envia a mensagem ele escuta no FraudeConsumerService e chama esse serviço
    public class FraudeService : IFraudeService
    {
        public async Task OnCreateAccountAsync(AccountDto dto)
        {
            await Task.Delay(200); 
            Console.WriteLine($"Conta criada - Verificação de fraude iniciada. Account Name: {dto.Name}");
        }
        public async Task OnUpdateAccountAsync(AccountDto dto)
        {
            await Task.Delay(200);
            Console.WriteLine($"Conta atualizada - Verificação de fraude iniciada. Account Name: {dto.Name}");
        }
        public async Task OnDeleteAccountAsync(AccountDto dto)
        {
            await Task.Delay(200);
            Console.WriteLine($"Conta excluída - Verificação de fraude iniciada. Account Name: {dto.Name}");
        }
    }
}
