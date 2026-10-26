using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.UserId).HasName("PK__Users__1788CCAC034B423B");
        builder.HasIndex(user => user.Email, "UQ__Users__A9D1053436B2B6C2").IsUnique();
        builder.Property(user => user.UserId).HasColumnName("UserID");
        builder.Property(user => user.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(user => user.Email).HasMaxLength(150);
        builder.Property(user => user.FirstName).HasMaxLength(100);
        builder.Property(user => user.IsActive).HasDefaultValue(true);
        builder.Property(user => user.LastName).HasMaxLength(100);
        builder.Property(user => user.PasswordHash).HasMaxLength(255);
        builder.Property(user => user.PositionId).HasColumnName("PositionID");
        builder.HasOne(user => user.Position).WithMany(position => position.Users)
            .HasForeignKey(user => user.PositionId).OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Users_Positions");
        builder.HasMany(user => user.Roles)
            .WithMany(role => role.Users)
            .UsingEntity<Dictionary<string, object>>(
                "UserRole",
                right => right.HasOne<Role>().WithMany().HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_UserRoles_Roles"),
                left => left.HasOne<User>().WithMany().HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_UserRoles_Users"),
                join =>
                {
                    join.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF27604FA565E84A");
                    join.ToTable("UserRoles");
                    join.IndexerProperty<int>("UserId").HasColumnName("UserID");
                    join.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                });
    }
}
