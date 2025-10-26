using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Entities;
using krt_api.Core.Accounts.Interfaces;
using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Utils;
using krt_api.Core.Utils.Enums;
using Microsoft.EntityFrameworkCore;

namespace krt_api.Infrastructure.Repositories
{
    public class AccountsRepository : Repository<Accounts>, IAccountsRepository 
    {
        public AccountsRepository(AppDbContext context) : base(context) { }

        public async Task<Accounts> GetByCPFAsync(string cpf)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.CPF == cpf && x.Status == AccountStatus.ACTIVE);
        }
        public async Task<bool> AnotherUserRegisterWithSameCPF(Guid id, string cpf)
        {
            return await _dbSet.AnyAsync(x => x.CPF == cpf && x.Id != id && x.Status == AccountStatus.ACTIVE);
        }
        public async Task<Accounts> GetByIdAsync(Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<ListAllAccountsResponseDto> GetAllAsync(string? filter = null, AccountStatus? status = null, 
            OrderBy orderBy = OrderBy.Descending,
            int page = 1)
        {
            IQueryable<Accounts> query = _dbSet.AsQueryable();
            int pageSize = 10;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                string filterToUpper = filter.Trim().ToUpper();
                query = query.Where(x =>
                    x.Name.ToUpper().Contains(filterToUpper) ||
                    x.CPF.ToUpper().Contains(filterToUpper));
            }

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            query = orderBy == OrderBy.Ascending
                ? query.OrderBy(x => x.CreatedAt)
                : query.OrderByDescending(x => x.CreatedAt);

            int totalCount = await query.CountAsync();

            var accounts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new ListAllAccountsResponseDto
            {
                Accounts = accounts,
                Paginate = new PaginateModel
                {
                    Page = page,
                    PageSize = pageSize,
                    PageCount = pageCount,
                    TotalCount = totalCount
                }
            };
        }

    }
}
