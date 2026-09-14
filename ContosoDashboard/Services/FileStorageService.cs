namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativeFolder);
    Task<bool> DeleteAsync(string filePath);
    Task<Stream> DownloadAsync(string filePath);
    Task<string> GetUrlAsync(string filePath, TimeSpan expiration);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseDirectory;

    public LocalFileStorageService()
    {
        _baseDirectory = Path.Combine(AppContext.BaseDirectory, "AppData", "uploads");
        Directory.CreateDirectory(_baseDirectory);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativeFolder)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid():N}{extension}";

        var folderPath = Path.Combine(_baseDirectory, relativeFolder.TrimStart('/').Replace('/', Path.DirectorySeparatorChar).Trim());
        Directory.CreateDirectory(folderPath);

        var targetPath = Path.Combine(folderPath, uniqueName);
        await using var targetStream = File.Create(targetPath);
        await fileStream.CopyToAsync(targetStream);

        return Path.Combine(relativeFolder.TrimStart('/'), uniqueName).Replace('\\', '/');
    }

    public Task<bool> DeleteAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(false);
        }

        var fullPath = Path.Combine(_baseDirectory, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public async Task<Stream> DownloadAsync(string filePath)
    {
        var fullPath = Path.Combine(_baseDirectory, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Document file not found.", fullPath);
        }

        return await Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task<string> GetUrlAsync(string filePath, TimeSpan expiration)
    {
        return Task.FromResult($"/documents/download?file={Uri.EscapeDataString(filePath)}");
    }
}
