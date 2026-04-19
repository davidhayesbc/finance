namespace Privestio.Contracts.Responses;

public record HouseholdMemberResponse
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}

public record HouseholdInvitationResponse
{
    public Guid Id { get; init; }
    public string InvitedEmail { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime InvitedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public record HouseholdResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public IReadOnlyList<HouseholdMemberResponse> Members { get; init; } = [];
    public IReadOnlyList<HouseholdInvitationResponse> PendingInvitations { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
