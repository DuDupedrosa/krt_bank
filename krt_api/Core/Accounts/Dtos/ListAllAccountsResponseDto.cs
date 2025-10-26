
using krt_api.Core.Utils;

namespace krt_api.Core.Accounts.Dtos
{
    public class ListAllAccountsResponseDto
    {
        public List<Entities.Accounts> Accounts { get; set; }
        public PaginateModel Paginate { get; set; }
    }
}
