using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.HasKey(position => position.PositionId).HasName("PK__Position__60BB9A5974C9B127");
        builder.Property(position => position.PositionId).HasColumnName("PositionID");
        builder.Property(position => position.Department).HasMaxLength(100);
        builder.Property(position => position.PositionName).HasMaxLength(100);
        builder.Property(position => position.ReportsToPositionId).HasColumnName("ReportsToPositionID");
        builder.HasOne(position => position.ReportsToPosition)
            .WithMany(position => position.Subordinates)
            .HasForeignKey(position => position.ReportsToPositionId)
            .HasConstraintName("FK_Positions_ReportsTo");
    }
}
