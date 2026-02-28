namespace Privestio.Domain.Entities;

/// <summary>
/// Many-to-many link between TransactionSplit and Tag.
/// </summary>
public class TransactionSplitTag
{
    private TransactionSplitTag() { }

    public TransactionSplitTag(Guid transactionSplitId, Guid tagId)
    {
        TransactionSplitId = transactionSplitId;
        TagId = tagId;
    }

    public Guid TransactionSplitId { get; private set; }
    public TransactionSplit? TransactionSplit { get; set; }

    public Guid TagId { get; private set; }
    public Tag? Tag { get; set; }
}
