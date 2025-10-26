using krt_api.Core.Accounts.Entities;
using krt_api.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace krt_api.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Accounts> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new AccountsConfiguration());
        }
    }
}
