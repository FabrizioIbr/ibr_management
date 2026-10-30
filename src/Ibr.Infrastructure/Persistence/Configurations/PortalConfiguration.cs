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

        // La tabla puente UserPortals usa INT y referencia la nueva tabla Usuarios (AppUser).
        builder.HasMany(portal => portal.Users)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UserPortal",
                right => right.HasOne<AppUser>().WithMany().HasForeignKey("UserId")
                    .HasConstraintName("FK_UserPortals_Users").OnDelete(DeleteBehavior.NoAction),
                left => left.HasOne<Portal>().WithMany().HasForeignKey("PortalId")
                    .HasConstraintName("FK_UserPortals_Portals").OnDelete(DeleteBehavior.NoAction),
                join =>
                {
                    join.HasKey("UserId", "PortalId").HasName("PK_UserPortals");
                    join.ToTable("UserPortals");
                    join.IndexerProperty<int>("UserId").HasColumnName("UserID");
                    join.IndexerProperty<int>("PortalId").HasColumnName("PortalID");
                    join.IndexerProperty<DateTime>("AssignedAt").HasDefaultValueSql("(getdate())");
                });
    }
}
