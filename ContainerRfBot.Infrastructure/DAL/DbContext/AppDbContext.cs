using ContainerRfBot.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerRfBot.Infrastructure.DAL.DbContext;

public partial class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // В ContainerFather конфигурации применялись из сборки, 
        // но также часто могут быть прямо в DbSets.cs
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}