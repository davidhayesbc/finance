namespace Privestio.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with an associated currency code.
/// Always use decimal for financial amounts; never float or double.
/// </summary>
public readonly record struct Money(decimal Amount, string CurrencyCode = "CAD")
{
    public static Money Zero(string currencyCode = "CAD") => new(0m, currencyCode);

    public Money Add(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot add {CurrencyCode} and {other.CurrencyCode}. Use explicit FX conversion.");
        return new(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot subtract {other.CurrencyCode} from {CurrencyCode}. Use explicit FX conversion.");
        return new(Amount - other.Amount, CurrencyCode);
    }

    public Money Negate() => this with { Amount = -Amount };

    public Money Abs() => this with { Amount = Math.Abs(Amount) };

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator -(Money value) => value.Negate();

    public static bool operator >(Money left, Money right)
    {
        AssertSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        AssertSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        AssertSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        AssertSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    private static void AssertSameCurrency(Money left, Money right)
    {
        if (left.CurrencyCode != right.CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot compare {left.CurrencyCode} and {right.CurrencyCode}.");
    }

    public override string ToString() => $"{Amount:F2} {CurrencyCode}";
}
