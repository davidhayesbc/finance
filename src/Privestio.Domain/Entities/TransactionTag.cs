namespace Privestio.Domain.Entities;

/// <summary>
/// Many-to-many link between Transaction and Tag.
/// </summary>
public class TransactionTag
{
    private TransactionTag() { }

    public TransactionTag(Guid transactionId, Guid tagId)
    {
        TransactionId = transactionId;
        TagId = tagId;
    }

    public Guid TransactionId { get; private set; }
    public Transaction? Transaction { get; set; }

    public Guid TagId { get; private set; }
    public Tag? Tag { get; set; }
}
