using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContainerRfBot.Infrastructure.DAL.DbContext;

public class UserConfiguration : IEntityTypeConfiguration<Entites.User>
{
    public void Configure(EntityTypeBuilder<Entites.User> builder)
    {
        // ContainerFather relies on ApplyConfigurationsFromAssembly, so we explicitly implement IEntityTypeConfiguration
        var context = new AppDbContext(new DbContextOptions<AppDbContext>());
        context.Configure(builder);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Entites.Message>
{
    public void Configure(EntityTypeBuilder<Entites.Message> builder)
    {
        var context = new AppDbContext(new DbContextOptions<AppDbContext>());
        context.Configure(builder);
    }
}