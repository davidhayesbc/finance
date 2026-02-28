using FluentAssertions;
using FluentValidation.TestHelper;
using Privestio.Application.Commands.CreateAccount;
using Xunit;

namespace Privestio.Application.Tests.Validators;

public class CreateAccountCommandValidatorTests
{
    private readonly CreateAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var command = new CreateAccountCommand(
            Name: "Test Account",
            AccountType: "Banking",
            AccountSubType: "Chequing",
            Currency: "CAD",
            OpeningBalance: 0m,
            OpeningDate: DateTime.UtcNow.AddYears(-1),
            OwnerId: Guid.NewGuid());

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_HasError(string name)
    {
        var command = new CreateAccountCommand(
            Name: name,
            AccountType: "Banking",
            AccountSubType: "Chequing",
            Currency: "CAD",
            OpeningBalance: 0m,
            OpeningDate: DateTime.UtcNow,
            OwnerId: Guid.NewGuid());

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_InvalidAccountType_HasError()
    {
        var command = new CreateAccountCommand(
            Name: "Test",
            AccountType: "InvalidType",
            AccountSubType: "Chequing",
            Currency: "CAD",
            OpeningBalance: 0m,
            OpeningDate: DateTime.UtcNow,
            OwnerId: Guid.NewGuid());

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AccountType);
    }

    [Theory]
    [InlineData("CA")]
    [InlineData("CAAD")]
    [InlineData("cad")]
    public void Validate_InvalidCurrencyCode_HasError(string currency)
    {
        var command = new CreateAccountCommand(
            Name: "Test",
            AccountType: "Banking",
            AccountSubType: "Chequing",
            Currency: currency,
            OpeningBalance: 0m,
            OpeningDate: DateTime.UtcNow,
            OwnerId: Guid.NewGuid());

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Validate_FutureOpeningDate_HasError()
    {
        var command = new CreateAccountCommand(
            Name: "Test",
            AccountType: "Banking",
            AccountSubType: "Chequing",
            Currency: "CAD",
            OpeningBalance: 0m,
            OpeningDate: DateTime.UtcNow.AddDays(10),
            OwnerId: Guid.NewGuid());

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.OpeningDate);
    }
}
