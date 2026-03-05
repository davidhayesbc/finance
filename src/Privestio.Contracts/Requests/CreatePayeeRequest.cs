namespace Privestio.Contracts.Requests;

public record CreatePayeeRequest(string DisplayName, Guid? DefaultCategoryId = null);
