using Bogus;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Infrastructure.Seeding;

/// <summary>
/// Generates realistic seed data for development and demo mode using Bogus.
/// </summary>
public static class SeedDataGenerator
{
    private static readonly Faker _faker = new("en_CA");

    public static List<Category> GenerateDefaultCategories(Guid? ownerId = null)
    {
        return
        [
            // Income categories
            new Category("Employment Income", CategoryType.Income, ownerId, null, "💼", 1, true),
            new Category("Investment Income", CategoryType.Income, ownerId, null, "📈", 2, true),
            new Category("Other Income", CategoryType.Income, ownerId, null, "💰", 3, true),

            // Expense categories
            new Category("Housing", CategoryType.Expense, ownerId, null, "🏠", 10, true),
            new Category("Groceries", CategoryType.Expense, ownerId, null, "🛒", 11, true),
            new Category("Dining Out", CategoryType.Expense, ownerId, null, "🍽️", 12, true),
            new Category("Transportation", CategoryType.Expense, ownerId, null, "🚗", 13, true),
            new Category("Healthcare", CategoryType.Expense, ownerId, null, "🏥", 14, true),
            new Category("Entertainment", CategoryType.Expense, ownerId, null, "🎬", 15, true),
            new Category("Shopping", CategoryType.Expense, ownerId, null, "🛍️", 16, true),
            new Category("Utilities", CategoryType.Expense, ownerId, null, "💡", 17, true),
            new Category("Insurance", CategoryType.Expense, ownerId, null, "🛡️", 18, true),
            new Category("Subscriptions", CategoryType.Expense, ownerId, null, "📱", 19, true),
            new Category("Personal Care", CategoryType.Expense, ownerId, null, "💇", 20, true),
            new Category("Education", CategoryType.Expense, ownerId, null, "📚", 21, true),
            new Category("Travel", CategoryType.Expense, ownerId, null, "✈️", 22, true),
            new Category("Gifts & Donations", CategoryType.Expense, ownerId, null, "🎁", 23, true),
            new Category("Other Expenses", CategoryType.Expense, ownerId, null, "📋", 99, true),

            // Transfer
            new Category("Transfer", CategoryType.Transfer, ownerId, null, "↔️", 50, true),
        ];
    }

    public static (User user, List<Account> accounts, List<Transaction> transactions) GenerateSampleData(
        string email = "demo@privestio.app",
        string displayName = "Demo User")
    {
        var user = new User(email, displayName);

        var chequingAccount = new Account(
            "Main Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(2500.00m),
            DateTime.UtcNow.AddYears(-2),
            user.Id,
            "RBC");

        var savingsAccount = new Account(
            "High-Interest Savings",
            AccountType.Banking,
            AccountSubType.Savings,
            "CAD",
            new Money(10000.00m),
            DateTime.UtcNow.AddYears(-2),
            user.Id,
            "RBC");

        var creditCard = new Account(
            "Visa Infinite",
            AccountType.Credit,
            AccountSubType.CreditCard,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            user.Id,
            "RBC");

        var rrsp = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(25000.00m),
            DateTime.UtcNow.AddYears(-5),
            user.Id,
            "TD");

        var accounts = new List<Account> { chequingAccount, savingsAccount, creditCard, rrsp };

        var transactions = GenerateSampleTransactions(chequingAccount, creditCard, 90);

        return (user, accounts, transactions);
    }

    private static List<Transaction> GenerateSampleTransactions(
        Account chequingAccount,
        Account creditCard,
        int daysBack)
    {
        var transactions = new List<Transaction>();
        var random = new Random(42); // Fixed seed for reproducibility

        var payees = new[]
        {
            "Loblaws", "Metro", "Costco", "Walmart", "Tim Hortons", "Starbucks",
            "McDonald's", "Amazon", "Netflix", "Spotify", "Rogers", "Bell",
            "Hydro One", "Shell", "Petro-Canada", "LCBO", "Chapters",
            "Sport Chek", "Rexall", "Shoppers Drug Mart",
        };

        // Bi-weekly salary
        for (var days = daysBack; days >= 0; days -= 14)
        {
            var date = DateTime.UtcNow.AddDays(-days);
            var salary = new Transaction(
                chequingAccount.Id,
                date,
                new Money(3500.00m),
                "PAYROLL DEPOSIT - EMPLOYER CO",
                TransactionType.Credit);
            transactions.Add(salary);
        }

        // Random expenses over the period
        for (var days = daysBack; days >= 0; days--)
        {
            var numTransactions = random.Next(0, 4);
            for (var i = 0; i < numTransactions; i++)
            {
                var payee = payees[random.Next(payees.Length)];
                var amount = Math.Round((decimal)(random.NextDouble() * 150 + 5), 2);
                var date = DateTime.UtcNow.AddDays(-days);
                var account = random.Next(2) == 0 ? chequingAccount : creditCard;

                var transaction = new Transaction(
                    account.Id,
                    date,
                    new Money(amount),
                    payee.ToUpperInvariant(),
                    TransactionType.Debit);

                transactions.Add(transaction);
            }
        }

        return transactions;
    }
}
