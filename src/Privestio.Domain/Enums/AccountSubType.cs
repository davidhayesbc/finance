namespace Privestio.Domain.Enums;

/// <summary>
/// Specific subtype of an account for richer classification.
/// </summary>
public enum AccountSubType
{
    // Banking
    Chequing = 1,
    Savings = 2,

    // Credit
    CreditCard = 10,
    LineOfCredit = 11,

    // Investment (Registered)
    RRSP = 20,
    TFSA = 21,
    RESP = 22,
    LIRA = 23,

    // Investment (Non-Registered)
    NonRegistered = 30,

    // Property
    RealEstate = 40,
    Vehicle = 41,
    OtherAsset = 42,

    // Loan
    Mortgage = 50,
    AutoLoan = 51,
    StudentLoan = 52,
    PersonalLoan = 53,
}
