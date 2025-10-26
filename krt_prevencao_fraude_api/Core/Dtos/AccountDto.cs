using krt_prevencao_fraude_api.Core.Utils;

namespace krt_prevencao_fraude_api.Core.Dtos
{
    public class AccountDto
    {
        public Guid Id { get; set; } 
        public DateTime CreatedAt { get; set; } 
        public DateTime UpdatedAt { get; set; } 
        public string Name { get; set; }
        public string CPF { get; set; }
        public AccountStatus Status { get; set; }
    }
}
