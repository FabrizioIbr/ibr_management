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
        builder.HasMany(role => role.Permissions)
            .WithMany(permission => permission.Roles)
            .UsingEntity<Dictionary<string, object>>(
                "RolePermission",
                right => right.HasOne<Permission>().WithMany().HasForeignKey("PermissionId")
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_RolePermissions_Permissions"),
                left => left.HasOne<Role>().WithMany().HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_RolePermissions_Roles"),
                join =>
                {
                    join.HasKey("RoleId", "PermissionId").HasName("PK__RolePerm__6400A18A3FA67306");
                    join.ToTable("RolePermissions");
                    join.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    join.IndexerProperty<int>("PermissionId").HasColumnName("PermissionID");
                });
    }
}
