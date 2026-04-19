using FluentAssertions;
using Moq;
using Privestio.Application.Commands.AcceptHouseholdInvitation;
using Privestio.Application.Commands.CreateHousehold;
using Privestio.Application.Commands.RemoveHouseholdMember;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Tests.Commands;

public class HouseholdCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHouseholdRepository> _householdRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;

    public HouseholdCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _householdRepositoryMock = new Mock<IHouseholdRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _unitOfWorkMock.SetupGet(x => x.Households).Returns(_householdRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateHousehold_SetsOwnerHouseholdId()
    {
        var owner = new User("owner@example.com", "Owner");
        Household? createdHousehold = null;

        _householdRepositoryMock
            .Setup(x => x.IsUserInAnyHouseholdAsync(owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _householdRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
            .Callback<Household, CancellationToken>((household, _) => createdHousehold = household)
            .ReturnsAsync((Household household, CancellationToken _) => household);
        _householdRepositoryMock
            .Setup(x => x.GetByIdWithMembersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdHousehold);

        var handler = new CreateHouseholdCommandHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(
            new CreateHouseholdCommand("Household", owner.Id),
            CancellationToken.None
        );

        owner.HouseholdId.Should().Be(createdHousehold!.Id);
        result.Id.Should().Be(createdHousehold.Id);
    }

    [Fact]
    public async Task AcceptHouseholdInvitation_SetsAcceptedUsersHouseholdId()
    {
        var owner = new User("owner@example.com", "Owner");
        var member = new User("member@example.com", "Member");
        var household = new Household("Household", owner.Id);
        var invitation = household.CreateInvitation(member.Email, HouseholdRole.Member, owner.Id);
        invitation.Household = household;

        _householdRepositoryMock
            .Setup(x => x.IsUserInAnyHouseholdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _householdRepositoryMock
            .Setup(x => x.GetInvitationByTokenAsync(invitation.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _householdRepositoryMock
            .Setup(x => x.UpdateAsync(household, It.IsAny<CancellationToken>()))
            .ReturnsAsync(household);
        _householdRepositoryMock
            .Setup(x => x.GetByIdWithMembersAsync(household.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(household);

        var handler = new AcceptHouseholdInvitationCommandHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(
            new AcceptHouseholdInvitationCommand(invitation.Token, member.Id, member.Email),
            CancellationToken.None
        );

        member.HouseholdId.Should().Be(household.Id);
        result.Id.Should().Be(household.Id);
    }

    [Fact]
    public async Task RemoveHouseholdMember_ClearsRemovedUsersHouseholdId()
    {
        var owner = new User("owner@example.com", "Owner");
        var member = new User("member@example.com", "Member");
        var household = new Household("Household", owner.Id);
        household.AddMember(member.Id, HouseholdRole.Member);
        member.HouseholdId = household.Id;

        _householdRepositoryMock
            .Setup(x => x.GetByIdWithMembersAsync(household.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(household);
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _householdRepositoryMock
            .Setup(x => x.UpdateAsync(household, It.IsAny<CancellationToken>()))
            .ReturnsAsync(household);

        var handler = new RemoveHouseholdMemberCommandHandler(
            _unitOfWorkMock.Object,
            new ResourcePermissionService(_unitOfWorkMock.Object)
        );

        await handler.Handle(
            new RemoveHouseholdMemberCommand(household.Id, member.Id, member.Id),
            CancellationToken.None
        );

        member.HouseholdId.Should().BeNull();
    }
}
