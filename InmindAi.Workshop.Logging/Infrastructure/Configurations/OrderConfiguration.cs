using InmindAi.Workshop.Logging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InmindAi.Workshop.Logging.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Reference)
            .IsUnique();
        builder.HasMany(x => x.OrderLines)
            .WithOne()
            .HasForeignKey(x => x.OrderId);
    }
}
