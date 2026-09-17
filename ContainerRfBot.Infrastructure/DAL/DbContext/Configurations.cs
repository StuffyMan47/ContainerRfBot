using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContainerRfBot.Infrastructure.DAL.DbContext;

public class UserConfiguration : IEntityTypeConfiguration<Entites.User>
{
    public void Configure(EntityTypeBuilder<Entites.User> builder)
    {
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

public class ContainerConfiguration : IEntityTypeConfiguration<Entites.Container>
{
    public void Configure(EntityTypeBuilder<Entites.Container> builder)
    {
        var context = new AppDbContext(new DbContextOptions<AppDbContext>());
        context.Configure(builder);
    }
}
