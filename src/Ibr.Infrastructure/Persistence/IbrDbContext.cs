using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Persistence;

public class IbrDbContext(DbContextOptions<IbrDbContext> options) : DbContext(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Portal> Portals => Set<Portal>();
    public DbSet<PortalSection> PortalSections => Set<PortalSection>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IbrDbContext).Assembly);
    }
}
