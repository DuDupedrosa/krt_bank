using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Entities;

namespace krt_api.Core.Accounts.Entities
{
    public class Accounts : BaseEntity
    {
        public string Name { get; set; }
        public string CPF { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.ACTIVE;
    }
}
