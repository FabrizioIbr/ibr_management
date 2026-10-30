using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(role => role.RoleId).HasName("PK__Roles__8AFACE3A25716BF8");
        builder.HasIndex(role => role.RoleName, "UQ__Roles__8A2B6160206D7082").IsUnique();
        builder.Property(role => role.RoleId).HasColumnName("RoleID");
        builder.Property(role => role.Description).HasMaxLength(255);
        builder.Property(role => role.RoleName).HasMaxLength(50);
    }
}
