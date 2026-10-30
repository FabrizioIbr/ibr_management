using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class VacationRequestConfiguration : IEntityTypeConfiguration<VacationRequest>
{
    public void Configure(EntityTypeBuilder<VacationRequest> builder)
    {
        builder.ToTable("Solicitudes_Vacaciones");
        builder.HasKey(request => request.VacationId).HasName("PK_Solicitudes_Vacaciones");
        builder.Property(request => request.VacationId).HasColumnName("VacationID").ValueGeneratedNever();
        builder.Property(request => request.EmployeeId).HasColumnName("EmployeeID");
        builder.Property(request => request.StartDate).HasColumnType("date");
        builder.Property(request => request.EndDate).HasColumnType("date");
        builder.Property(request => request.CalendarDays).HasColumnType("decimal(10,2)");
        builder.Property(request => request.WorkingDays).HasColumnType("decimal(10,2)");
        builder.Property(request => request.Status).HasMaxLength(50).IsUnicode(false);
        builder.Property(request => request.VacationTypeId).HasColumnName("VacationTypeID");
        builder.Property(request => request.RequestedAt);
        builder.Property(request => request.ApprovedAt);
        builder.Property(request => request.CreatedAt).HasDefaultValueSql("(getdate())");
        builder.Property(request => request.RequiereAuditoria).HasDefaultValue(false);
        builder.Property(request => request.FechaDeteccionAnulacion);

        builder.HasOne(request => request.Employee)
            .WithMany()
            .HasForeignKey(request => request.EmployeeId)
            .HasConstraintName("FK__Solicitudes_Vacaciones__EmployeeID")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
