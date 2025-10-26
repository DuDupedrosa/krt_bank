using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using krt_api.Core.Accounts.Entities;

namespace krt_api.Infrastructure.Configurations
{
    public class AccountsConfiguration : IEntityTypeConfiguration<Accounts>
    {
        public void Configure(EntityTypeBuilder<Accounts> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.CPF)
                   .IsRequired()
                   .HasMaxLength(11);

            builder.Property(e => e.Status)
                    .IsRequired();

            builder.HasIndex(e => e.CPF)
                   .IsUnique()
                   .HasFilter("\"Status\" = 1");
        }
    }
}
