using Privestio.Contracts.Responses;

namespace Privestio.Web.Helpers;

public static class TransactionFormatHelper
{
    public static string FormatAmount(TransactionResponse transaction)
    {
        var prefix = transaction.TransactionType switch
        {
            "Credit" => "+",
            "Debit" => "-",
            "Transfer" => transaction.Amount < 0 ? "-" : "+",
            _ => string.Empty,
        };

        return $"{prefix}{Math.Abs(transaction.Amount):C} {transaction.Currency}";
    }

    public static string GetAmountClass(TransactionResponse transaction) =>
        transaction.TransactionType switch
        {
            "Credit" => "transaction-amount--positive",
            "Debit" => "transaction-amount--negative",
            "Transfer" => transaction.Amount < 0
                ? "transaction-amount--negative"
                : "transaction-amount--positive",
            _ => string.Empty,
        };
}
