using Microsoft.Extensions.Configuration;
using Quote.Application.Common.Interfaces;

namespace Quote.Infrastructure.Services;

public class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalBlobStorageService(IConfiguration configuration)
    {
        _basePath = configuration["Storage:LocalPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = configuration["Storage:BaseUrl"] ?? "/uploads";

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var uniqueName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
        var subFolder = GetSubFolder(contentType);
        var folderPath = Path.Combine(_basePath, subFolder);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, uniqueName);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fs, cancellationToken);

        return $"{_baseUrl}/{subFolder}/{uniqueName}";
    }

    public Task<Stream?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var relativePath = blobUrl.Replace(_baseUrl, "").TrimStart('/');
        var filePath = Path.Combine(_basePath, relativePath);

        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var relativePath = blobUrl.Replace(_baseUrl, "").TrimStart('/');
        var filePath = Path.Combine(_basePath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public string GetBlobUrl(string blobName) => $"{_baseUrl}/{blobName}";

    private static string GetSubFolder(string contentType)
    {
        if (contentType.StartsWith("image/")) return "images";
        if (contentType == "application/pdf") return "documents";
        return "misc";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
