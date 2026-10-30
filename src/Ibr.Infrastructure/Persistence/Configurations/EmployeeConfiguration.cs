using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Empleados");
        builder.HasKey(employee => employee.EmployeeId).HasName("PK_Empleados");
        builder.Property(employee => employee.EmployeeId).HasColumnName("EmployeeID").ValueGeneratedNever();
        builder.Property(employee => employee.PersonId).HasColumnName("PersonID");
        builder.Property(employee => employee.CompanyId).HasColumnName("CompanyID");
        builder.Property(employee => employee.CodeSheet).HasColumnName("CodeSheet").HasMaxLength(50).IsUnicode(false);
        builder.Property(employee => employee.FirstName).HasMaxLength(100);
        builder.Property(employee => employee.Surname).HasMaxLength(100);
        builder.Property(employee => employee.SecondSurname).HasMaxLength(100);
        builder.Property(employee => employee.FullName).HasMaxLength(200);
        builder.Property(employee => employee.Email).HasMaxLength(150);
        builder.Property(employee => employee.HireDate).HasColumnType("date");
        builder.Property(employee => employee.Status).HasMaxLength(50).IsUnicode(false);
        builder.Property(employee => employee.AreaId).HasColumnName("AreaID");
        builder.Property(employee => employee.CargoId).HasColumnName("CargoID");
        builder.Property(employee => employee.BossId).HasColumnName("BossID");
        builder.Property(employee => employee.LastSyncAt).HasDefaultValueSql("(getdate())");
        builder.Property(employee => employee.FechaCese).HasColumnType("date");
        builder.Property(employee => employee.MotivoCese).HasMaxLength(100).IsUnicode(false);
        builder.Property(employee => employee.TipoContrato).HasMaxLength(100).IsUnicode(false);
        builder.Property(employee => employee.RegimenLaboral).HasMaxLength(50).IsUnicode(false);
        builder.Property(employee => employee.JornadaHorasSemana);
        builder.Property(employee => employee.DiasLaboralesSemana);
        builder.Property(employee => employee.DiasLaboralesDetalle).HasMaxLength(10).IsUnicode(false);
        builder.Property(employee => employee.DiasVacacionesAnuales).HasColumnType("decimal(5,2)");
        builder.Property(employee => employee.VacacionesProgresivasDesde).HasColumnType("date");
        builder.Property(employee => employee.SaldoActualBuk).HasColumnType("decimal(10,2)");

        builder.HasOne(employee => employee.Boss)
            .WithMany()
            .HasForeignKey(employee => employee.BossId)
            .HasConstraintName("FK_Empleados_Boss")
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(employee => employee.Company)
            .WithMany()
            .HasForeignKey(employee => employee.CompanyId)
            .HasConstraintName("FK_Empleados_Companies")
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(employee => employee.Area)
            .WithMany()
            .HasForeignKey(employee => employee.AreaId)
            .HasConstraintName("FK_Empleados_Areas")
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(employee => employee.Cargo)
            .WithMany()
            .HasForeignKey(employee => employee.CargoId)
            .HasConstraintName("FK_Empleados_Cargos")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
