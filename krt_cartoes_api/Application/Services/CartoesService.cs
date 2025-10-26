using krt_cartoes_api.Core.Dtos;
using krt_cartoes_api.Core.Interfaces;

namespace krt_cartoes_api.Application.Services
{
    // serviço responsável por lidar com a lógica de cartões
    // como é só um exemplo para ser utilizado no consumer, a lógica será apenas um Console.WriteLine
    // mas está totalmente funcional, assim que o produtor envia a mensagem ele escuta no CartoesConsumerService e chama esse serviço
    public class CartoesService : ICartoesService
    {
        public async Task OnCreateAccountAsync(AccountDto dto)
        {
            await Task.Delay(200);
            Console.WriteLine($"Conta criada - Lógica para criar o cartão pronta para ser executada. Account Name: {dto.Name}");
        }
        public async Task OnUpdateAccountAsync(AccountDto dto)
        {
            await Task.Delay(200);
            Console.WriteLine($"Conta atualizada - Lógica para atualizar o cartão pronta para ser executada. Account Name: {dto.Name}");
        }
        public async Task OnDeleteAccountAsync(AccountDto dto)
        {
            await Task.Delay(200);
            Console.WriteLine($"Conta deletada - Lógica para bloquear/remover o cartão pronta para ser executada. Account Name: {dto.Name}");
        }
    }
}
