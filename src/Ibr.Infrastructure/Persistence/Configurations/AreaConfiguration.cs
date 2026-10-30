using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas");
        builder.HasKey(area => area.AreaId).HasName("PK_Areas");
        builder.Property(area => area.AreaId).HasColumnName("AreaID").ValueGeneratedNever();
        builder.Property(area => area.Name).HasMaxLength(200);
        builder.Property(area => area.Status).HasMaxLength(50).IsUnicode(false);
        builder.Property(area => area.ParentAreaId).HasColumnName("ParentAreaID");
        builder.Property(area => area.DepartmentId).HasColumnName("DepartmentID");
        builder.Property(area => area.DepartmentName).HasMaxLength(200);
        builder.Property(area => area.DivisionId).HasColumnName("DivisionID");
        builder.Property(area => area.DivisionName).HasMaxLength(200);
        builder.Property(area => area.CostCenter).HasMaxLength(50).IsUnicode(false);
        builder.Property(area => area.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(area => area.UpdatedAt).HasDefaultValueSql("(getdate())");

        builder.HasOne(area => area.ParentArea)
            .WithMany(area => area.Children)
            .HasForeignKey(area => area.ParentAreaId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
