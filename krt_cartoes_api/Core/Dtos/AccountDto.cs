using krt_cartoes_api.Core.Utils;

namespace krt_cartoes_api.Core.Dtos
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
