using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class VacationPlanConfiguration : IEntityTypeConfiguration<VacationPlan>
{
    public void Configure(EntityTypeBuilder<VacationPlan> builder)
    {
        builder.ToTable("Planificacion_Vacaciones");
        builder.HasKey(plan => plan.Id).HasName("PK_Planificacion_Vacaciones");
        builder.Property(plan => plan.Id).ValueGeneratedOnAdd();
        builder.Property(plan => plan.EmployeeId).HasColumnName("EmployeeID");
        builder.Property(plan => plan.FechaInicioPropuesta).HasColumnType("date");
        builder.Property(plan => plan.FechaFinPropuesta).HasColumnType("date");
        builder.Property(plan => plan.DiasPropuestos).HasColumnType("decimal(10,2)");
        builder.Property(plan => plan.Estado).HasMaxLength(50).IsUnicode(false).HasDefaultValueSql("('Planificado')");
        builder.Property(plan => plan.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(plan => plan.UpdatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(plan => plan.CreatedByEmail).HasMaxLength(150).IsUnicode(false);
        builder.Property(plan => plan.CreatedByEmployeeId).HasColumnName("CreatedByEmployeeID");

        builder.HasOne(plan => plan.Employee)
            .WithMany()
            .HasForeignKey(plan => plan.EmployeeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(plan => plan.CreatedByEmployee)
            .WithMany()
            .HasForeignKey(plan => plan.CreatedByEmployeeId)
            .HasConstraintName("FK_Planificacion_Creador")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
