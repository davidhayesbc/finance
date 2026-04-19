using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Mapping;

public static class HouseholdMapper
{
    public static HouseholdResponse ToResponse(Household household) =>
        new()
        {
            Id = household.Id,
            Name = household.Name,
            OwnerId = household.OwnerId,
            Members = household.Members.Select(ToMemberResponse).ToList().AsReadOnly(),
            PendingInvitations = household
                .Invitations.Where(i => i.Status == HouseholdInvitationStatus.Pending)
                .Select(ToInvitationResponse)
                .ToList()
                .AsReadOnly(),
            CreatedAt = household.CreatedAt,
            UpdatedAt = household.UpdatedAt,
        };

    public static HouseholdMemberResponse ToMemberResponse(HouseholdMember member) =>
        new()
        {
            UserId = member.UserId,
            DisplayName = member.User?.DisplayName ?? string.Empty,
            Email = member.User?.Email ?? string.Empty,
            Role = member.Role.ToString(),
            JoinedAt = member.JoinedAt,
        };

    public static HouseholdInvitationResponse ToInvitationResponse(HouseholdInvitation invitation) =>
        new()
        {
            Id = invitation.Id,
            InvitedEmail = invitation.InvitedEmail,
            Role = invitation.Role.ToString(),
            Status = invitation.Status.ToString(),
            InvitedAt = invitation.InvitedAt,
            ExpiresAt = invitation.ExpiresAt,
        };
}
