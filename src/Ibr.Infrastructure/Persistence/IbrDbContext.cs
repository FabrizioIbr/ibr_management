using Ibr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Persistence;

public class IbrDbContext(DbContextOptions<IbrDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<VacationPeriod> VacationPeriods => Set<VacationPeriod>();
    public DbSet<VacationPlan> VacationPlans => Set<VacationPlan>();
    public DbSet<VacationRequest> VacationRequests => Set<VacationRequest>();
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
