using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(int userId, string title, string category, string? description, int? projectId, string? tags, string fileName, string contentType, Stream fileStream, string sourceType, long fileSizeBytes);
    Task<List<Document>> GetUserDocumentsAsync(int userId);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif"
    };

    public DocumentService(ApplicationDbContext context, IFileStorageService fileStorageService, INotificationService notificationService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    public async Task<Document> UploadDocumentAsync(int userId, string title, string category, string? description, int? projectId, string? tags, string fileName, string contentType, Stream fileStream, string sourceType, long fileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Document category is required.");
        }

        if (fileStream == null || fileSizeBytes <= 0)
        {
            throw new ArgumentException("File content is required.");
        }

        if (fileSizeBytes > 25 * 1024 * 1024)
        {
            throw new ArgumentException("File exceeds the 25 MB size limit.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Unsupported file type.");
        }

        if (projectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId.Value);

            if (project == null)
            {
                throw new ArgumentException("Project not found.");
            }

            var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == userId) || project.ProjectManagerId == userId;
            if (!isProjectMember)
            {
                throw new UnauthorizedAccessException("You do not have access to upload documents to this project.");
            }
        }

        var relativeFolder = projectId.HasValue
            ? $"{userId}/{projectId.Value}"
            : $"{userId}/personal";

        var storedPath = await _fileStorageService.UploadAsync(fileStream, fileName, contentType, relativeFolder);

        var document = new Document
        {
            Title = title.Trim(),
            Description = description,
            Category = category,
            UploadedByUserId = userId,
            ProjectId = projectId,
            FileName = fileName,
            FileType = contentType,
            FileSizeBytes = fileSizeBytes,
            FilePath = storedPath,
            Tags = tags,
            UploadDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<List<Document>> GetUserDocumentsAsync(int userId)
    {
        return await _context.Documents
            .Where(d => d.UploadedByUserId == userId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
        {
            return new List<Document>();
        }

        var isAuthorized = project.ProjectManagerId == requestingUserId ||
                           project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isAuthorized)
        {
            return new List<Document>();
        }

        return await _context.Documents
            .Where(d => d.ProjectId == projectId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return null;
        }

        var userIsOwner = document.UploadedByUserId == requestingUserId;
        var userIsProjectManager = document.Project != null && document.Project.ProjectManagerId == requestingUserId;
        var userIsProjectMember = document.Project != null && document.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);
        var userHasSpecificShare = document.Shares.Any(s => s.UserId == requestingUserId);

        if (!userIsOwner && !userIsProjectManager && !userIsProjectMember && !userHasSpecificShare)
        {
            return null;
        }

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return false;
        }

        var userIsOwner = document.UploadedByUserId == requestingUserId;
        var userIsProjectManager = document.Project != null && document.Project.ProjectManagerId == requestingUserId;

        if (!userIsOwner && !userIsProjectManager)
        {
            return false;
        }

        document.IsDeleted = true;
        document.UpdatedDate = DateTime.UtcNow;

        await _fileStorageService.DeleteAsync(document.FilePath);
        await _context.SaveChangesAsync();

        return true;
    }
}
