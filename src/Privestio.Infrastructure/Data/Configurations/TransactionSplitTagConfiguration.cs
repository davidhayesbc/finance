using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class TransactionSplitTagConfiguration : IEntityTypeConfiguration<TransactionSplitTag>
{
    public void Configure(EntityTypeBuilder<TransactionSplitTag> builder)
    {
        builder.HasKey(tst => new { tst.TransactionSplitId, tst.TagId });

        builder.HasOne(tst => tst.TransactionSplit)
            .WithMany(s => s.Tags)
            .HasForeignKey(tst => tst.TransactionSplitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tst => tst.Tag)
            .WithMany(t => t.TransactionSplitTags)
            .HasForeignKey(tst => tst.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
