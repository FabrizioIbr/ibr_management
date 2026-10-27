using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibr.Infrastructure.Persistence.Configurations;

public class PortalSectionConfiguration : IEntityTypeConfiguration<PortalSection>
{
    public void Configure(EntityTypeBuilder<PortalSection> builder)
    {
        builder.HasKey(section => section.SectionId).HasName("PK__PortalSe__80EF08722B9D3BD3");
        builder.ToTable("PortalSections");
        builder.Property(section => section.Title).HasMaxLength(100);
        builder.Property(section => section.Description).HasMaxLength(255);
        builder.Property(section => section.Route).HasMaxLength(255);
        builder.Property(section => section.Status).HasMaxLength(50);
        builder.Property(section => section.SectionGroup).HasMaxLength(100);
        builder.HasOne(section => section.Portal).WithMany(portal => portal.Sections)
            .HasForeignKey(section => section.PortalId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PortalSections_Portals");
    }
}
