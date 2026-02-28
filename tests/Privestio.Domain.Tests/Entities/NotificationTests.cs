using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Domain.Tests.Entities;

public class NotificationTests
{
    private static Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_CreatesNotification()
    {
        var notification = new Notification(
            _userId,
            "BudgetExceeded",
            NotificationSeverity.Warning,
            "Budget Alert",
            "You have exceeded your groceries budget.");

        notification.UserId.Should().Be(_userId);
        notification.Type.Should().Be("BudgetExceeded");
        notification.Severity.Should().Be(NotificationSeverity.Warning);
        notification.Title.Should().Be("Budget Alert");
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MarkAsRead_UnreadNotification_SetsIsReadTrue()
    {
        var notification = new Notification(_userId, "Test", NotificationSeverity.Info, "Title", "Message");

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_DoesNotChangeReadAt()
    {
        var notification = new Notification(_userId, "Test", NotificationSeverity.Info, "Title", "Message");
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        notification.MarkAsRead();

        notification.ReadAt.Should().Be(firstReadAt);
    }

    [Fact]
    public void MarkAsUnread_ReadNotification_SetsIsReadFalse()
    {
        var notification = new Notification(_userId, "Test", NotificationSeverity.Info, "Title", "Message");
        notification.MarkAsRead();

        notification.MarkAsUnread();

        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }
}
