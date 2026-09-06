using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ContainerRfBot.Core.Entities;

namespace ContainerRfBot.Infrastructure.DAL.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(4000);
    }
}