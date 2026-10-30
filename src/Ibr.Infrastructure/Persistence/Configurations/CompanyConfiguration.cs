using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(company => company.CompanyId).HasName("PK_Companies");
        builder.Property(company => company.CompanyId).HasColumnName("CompanyID").ValueGeneratedNever();
        builder.Property(company => company.Name).HasMaxLength(200);
        builder.Property(company => company.Ruc).HasColumnName("RUC").HasMaxLength(20).IsUnicode(false);
        builder.Property(company => company.BusinessActivity);
        builder.Property(company => company.Email).HasMaxLength(150);
        builder.Property(company => company.Address);
        builder.Property(company => company.City).HasMaxLength(100);
        builder.Property(company => company.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(company => company.UpdatedAt).HasDefaultValueSql("(getdate())");
    }
}
