using EntityFramework.Exceptions.Sqlite;
using InmindAi.Workshop.Logging.Domain;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace InmindAi.Workshop.Logging.Infrastructure;

public class WorkShopDbContext : DbContext
{
    public WorkShopDbContext(DbContextOptions<WorkShopDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Handles common database exceptions using EntityFramework.Exceptions 
        optionsBuilder.UseExceptionProcessor();
        base.OnConfiguring(optionsBuilder);
    }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
}
