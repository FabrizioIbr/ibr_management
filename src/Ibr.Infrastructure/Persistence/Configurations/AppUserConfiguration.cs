using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(user => user.UserId).HasName("PK_Usuarios");
        builder.Property(user => user.UserId).HasColumnName("UserID");
        builder.Property(user => user.EmployeeId).HasColumnName("EmployeeID");
        builder.Property(user => user.Email).HasMaxLength(150);
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.EmailCorporativo).HasMaxLength(150);
        builder.Property(user => user.PasswordHash).HasMaxLength(255);
        builder.Property(user => user.AppRole).HasMaxLength(50).IsUnicode(false).HasDefaultValue("Empleado");
        builder.Property(user => user.IsActive).HasDefaultValue(true);
        builder.Property(user => user.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(user => user.UpdatedAt).HasDefaultValueSql("(getdate())");

        builder.HasOne(user => user.Employee)
            .WithMany()
            .HasForeignKey(user => user.EmployeeId)
            .HasConstraintName("FK__Usuarios__Employ__07C12930")
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasMany(user => user.Roles)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "Usuarios_Roles",
                right => right.HasOne<Role>().WithMany().HasForeignKey("RoleID")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<AppUser>().WithMany().HasForeignKey("UserID")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("UserID", "RoleID");
                    join.IndexerProperty<DateTime>("AssignedAt").HasDefaultValueSql("(getdate())");
                });
    }
}
