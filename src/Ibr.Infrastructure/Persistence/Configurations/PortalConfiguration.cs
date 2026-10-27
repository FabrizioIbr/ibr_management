using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class PortalConfiguration : IEntityTypeConfiguration<Portal>
{
    public void Configure(EntityTypeBuilder<Portal> builder)
    {
        builder.HasKey(portal => portal.PortalId).HasName("PK__Portals__B87D58133FF6E99F");
        builder.Property(portal => portal.Name).HasMaxLength(100);
        builder.Property(portal => portal.Description).HasMaxLength(255);
        builder.Property(portal => portal.ColorHex).HasMaxLength(7).IsUnicode(false);
        builder.Property(portal => portal.Status).HasMaxLength(50);
        builder.Property(portal => portal.CreatedAt).HasDefaultValueSql("(getdate())");

        // La columna es NVARCHAR(450) pero guarda el RoleID (int) de Roles: se convierte para poder relacionarla.
        builder.Property(portal => portal.RoleId).HasColumnName("RoleID").HasMaxLength(450).HasConversion<string>();
        builder.HasOne(portal => portal.Role).WithMany()
            .HasForeignKey(portal => portal.RoleId).OnDelete(DeleteBehavior.ClientSetNull);

        // UserPortals.UserId también es NVARCHAR(450) con el UserID (int) de Users.
        builder.HasMany(portal => portal.Users)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UserPortal",
                right => right.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Portal>().WithMany().HasForeignKey("PortalId")
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_UserPortals_Portals"),
                join =>
                {
                    join.HasKey("UserId", "PortalId").HasName("PK_UserPortals");
                    join.ToTable("UserPortals");
                    join.IndexerProperty<int>("UserId").HasMaxLength(450).HasConversion<string>();
                    join.IndexerProperty<DateTime>("GrantedAt").HasDefaultValueSql("(getdate())");
                });
    }
}
