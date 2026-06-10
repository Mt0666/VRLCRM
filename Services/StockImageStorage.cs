namespace VRLCRM.Services;

public interface IStockImageStorage
{
    Task<string?> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default);

    void Delete(string? imageUrl);
}

public class StockImageStorage : IStockImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _environment;

    public StockImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Görsel boyutu en fazla 5 MB olabilir.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Desteklenmeyen görsel formatı.");
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "stocks");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/stocks/{fileName}";
    }

    public void Delete(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/stocks/", StringComparison.Ordinal))
        {
            return;
        }

        var physicalPath = Path.Combine(
            _environment.WebRootPath,
            imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}
