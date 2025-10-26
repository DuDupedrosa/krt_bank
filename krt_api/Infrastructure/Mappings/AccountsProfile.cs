using AutoMapper;
using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Entities;

namespace krt_api.Infrastructure.Mappings
{
    public class AccountsProfile : Profile
    {
        public AccountsProfile()
        {
            CreateMap<CreateAccountDto, Accounts>();
        }
    }
}
