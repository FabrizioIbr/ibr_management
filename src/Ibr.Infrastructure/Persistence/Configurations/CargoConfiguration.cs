using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class CargoConfiguration : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargos");
        builder.HasKey(cargo => cargo.CargoId).HasName("PK_Cargos");
        builder.Property(cargo => cargo.CargoId).HasColumnName("CargoID").ValueGeneratedNever();
        builder.Property(cargo => cargo.Code).HasMaxLength(100).IsUnicode(false);
        builder.Property(cargo => cargo.Name).HasMaxLength(200);
        builder.Property(cargo => cargo.Description);
        builder.Property(cargo => cargo.Requirements);
        builder.Property(cargo => cargo.RoleFamilyId).HasColumnName("RoleFamilyID");
        builder.Property(cargo => cargo.RoleFamilyName).HasMaxLength(100);
        builder.Property(cargo => cargo.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(cargo => cargo.UpdatedAt).HasDefaultValueSql("(getdate())");

        builder.HasMany(cargo => cargo.Areas)
            .WithMany(area => area.Cargos)
            .UsingEntity<Dictionary<string, object>>(
                "CargoArea",
                right => right.HasOne<Area>().WithMany().HasForeignKey("AreaID")
                    .HasConstraintName("FK__Cargos_Ar__AreaI__7D439ABD"),
                left => left.HasOne<Cargo>().WithMany().HasForeignKey("CargoID")
                    .HasConstraintName("FK__Cargos_Ar__Cargo__7C4F7684"),
                join =>
                {
                    join.ToTable("Cargos_Areas");
                    join.HasKey("CargoID", "AreaID");
                });
    }
}
