using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(permission => permission.PermissionId).HasName("PK__Permissi__EFA6FB0F5AB02100");
        builder.HasIndex(permission => permission.PermissionName, "UQ__Permissi__0FFDA357917856FA").IsUnique();
        builder.Property(permission => permission.PermissionId).HasColumnName("PermissionID");
        builder.Property(permission => permission.Description).HasMaxLength(255);
        builder.Property(permission => permission.PermissionName).HasMaxLength(100);
    }
}
