using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ContosoDashboard.Tests;

public class DocumentServiceMvpTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        var admin = new User { UserId = 1, Email = "admin@contoso.com", DisplayName = "System Administrator", Role = UserRole.Administrator };
        var manager = new User { UserId = 2, Email = "camille.nicole@contoso.com", DisplayName = "Camille Nicole", Role = UserRole.ProjectManager };
        var teamLead = new User { UserId = 3, Email = "floris.kregel@contoso.com", DisplayName = "Floris Kregel", Role = UserRole.TeamLead };
        var employee = new User { UserId = 4, Email = "ni.kang@contoso.com", DisplayName = "Ni Kang", Role = UserRole.Employee };

        context.Users.AddRange(admin, manager, teamLead, employee);

        var project = new Project
        {
            ProjectId = 1,
            Name = "ContosoDashboard Development",
            Description = "Internal app",
            ProjectManagerId = 2,
            Status = ProjectStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            ProjectManager = manager
        };

        context.Projects.Add(project);

        context.ProjectMembers.Add(new ProjectMember
        {
            ProjectMemberId = 1,
            ProjectId = 1,
            UserId = 4,
            Role = "Developer",
            AssignedDate = DateTime.UtcNow
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task UploadDocument_ValidFile_CreatesMetadataAndStoresFile()
    {
        await using var context = CreateContext();
        var localStorage = new LocalFileStorageService();
        var service = new DocumentService(context, localStorage, new NullNotificationService());

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        var result = await service.UploadDocumentAsync(
            userId: 4,
            title: "Sprint Notes",
            category: "Project Documents",
            description: "Q3 plan",
            projectId: 1,
            tags: "sprint,planning",
            fileName: "notes.txt",
            contentType: "text/plain",
            fileStream: content,
            sourceType: "project",
            fileSizeBytes: content.Length);

        Assert.NotNull(result);
        Assert.Equal("Sprint Notes", result.Title);
        Assert.Equal(1, result.ProjectId);
        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(Path.Combine("AppData", "uploads", result.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task UploadDocument_UnsupportedType_ReturnsValidationFailure()
    {
        await using var context = CreateContext();
        var localStorage = new LocalFileStorageService();
        var service = new DocumentService(context, localStorage, new NullNotificationService());

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("bad"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            userId: 4,
            title: "Illegal File",
            category: "Project Documents",
            description: null,
            projectId: 1,
            tags: null,
            fileName: "malware.exe",
            contentType: "application/x-msdownload",
            fileStream: content,
            sourceType: "project",
            fileSizeBytes: content.Length));
    }

    [Fact]
    public async Task GetUserDocumentsAsync_UserWithoutAccess_OnlyReturnsAssignableFiles()
    {
        await using var context = CreateContext();
        var localStorage = new LocalFileStorageService();
        var service = new DocumentService(context, localStorage, new NullNotificationService());

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("file"));
        await service.UploadDocumentAsync(4, "Project file", "Project Documents", null, 1, "team", "project.txt", "text/plain", content, "project", content.Length);

        var documents = await service.GetUserDocumentsAsync(1);

        Assert.Empty(documents);
    }

    private sealed class NullNotificationService : INotificationService
    {
        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>());
        public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification);
        public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(true);
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
    }
}
