namespace Privestio.Contracts.Requests;

public record CreateHouseholdRequest(string Name);

public record InviteHouseholdMemberRequest(string Email, string Role);

public record AcceptHouseholdInvitationRequest(Guid Token);

public record UpdateHouseholdMemberRoleRequest(Guid UserId, string Role);

public record RenameHouseholdRequest(string Name);
