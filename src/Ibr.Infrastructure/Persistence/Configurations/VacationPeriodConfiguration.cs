using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class VacationPeriodConfiguration : IEntityTypeConfiguration<VacationPeriod>
{
    public void Configure(EntityTypeBuilder<VacationPeriod> builder)
    {
        builder.ToTable("Vacaciones_Periodos");
        builder.HasKey(period => period.PeriodId).HasName("PK_Vacaciones_Periodos");
        builder.HasIndex(period => new { period.EmployeeId, period.PeriodStartDate }).IsUnique();
        builder.Property(period => period.PeriodId).HasColumnName("PeriodID");
        builder.Property(period => period.EmployeeId).HasColumnName("EmployeeID");
        builder.Property(period => period.PeriodStartDate).HasColumnType("date");
        builder.Property(period => period.PeriodEndDate).HasColumnType("date");
        builder.Property(period => period.EarnedDays).HasColumnType("decimal(10,2)");
        builder.Property(period => period.IsProportional).HasDefaultValue(false);
        builder.Property(period => period.ExpirationDate).HasColumnType("date");
        builder.Property(period => period.CreatedAt).HasDefaultValueSql("(getdate())");

        builder.HasOne(period => period.Employee)
            .WithMany()
            .HasForeignKey(period => period.EmployeeId)
            .HasConstraintName("FK__Vacaciones_Periodos__EmployeeID")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
