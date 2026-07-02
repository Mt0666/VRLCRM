using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace VRLCRM.Services;

/// <summary>Kaydedilen görselin ana ve küçük (thumbnail) URL'leri.</summary>
public record StockImageResult(string ImageUrl, string ThumbnailUrl);

public interface IStockImageStorage
{
    Task<StockImageResult?> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default);

    /// <summary>Mevcut (daha önce yüklenmiş) bir görseli yerinde küçültür ve thumbnail üretir; thumbnail URL'ini döner.</summary>
    Task<string?> OptimizeExistingAsync(string? imageUrl, CancellationToken cancellationToken = default);

    void Delete(string? imageUrl);
}

public class StockImageStorage : IStockImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    /// <summary>Ana görselin en uzun kenarı bu piksele küçültülür.</summary>
    private const int MainMaxDimension = 1280;

    /// <summary>Listelerde kullanılan küçük görselin en uzun kenarı.</summary>
    private const int ThumbMaxDimension = 200;

    private readonly IWebHostEnvironment _environment;

    public StockImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<StockImageResult?> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default)
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
        var thumbsFolder = Path.Combine(uploadsFolder, "thumbs");
        Directory.CreateDirectory(uploadsFolder);
        Directory.CreateDirectory(thumbsFolder);

        var ext = extension.ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var mainPath = Path.Combine(uploadsFolder, fileName);
        var thumbPath = Path.Combine(thumbsFolder, fileName);

        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, cancellationToken);

            // Telefon fotoğraflarındaki EXIF yönünü uygula, sonra meta veriyi temizle.
            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;

            ResizeToMax(image, MainMaxDimension);
            await image.SaveAsync(mainPath, cancellationToken);

            // Thumbnail'i ana (küçültülmüş) görselden türet.
            ResizeToMax(image, ThumbMaxDimension);
            await image.SaveAsync(thumbPath, cancellationToken);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Görsel işlenemedi. Lütfen geçerli bir resim dosyası yükleyin.");
        }

        return new StockImageResult($"/uploads/stocks/{fileName}", $"/uploads/stocks/thumbs/{fileName}");
    }

    private static void ResizeToMax(Image image, int max)
    {
        if (image.Width <= max && image.Height <= max)
        {
            return; // Zaten küçük — büyütme yapma.
        }

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(max, max)
        }));
    }

    public async Task<string?> OptimizeExistingAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/stocks/", StringComparison.Ordinal))
        {
            return null;
        }

        var fileName = Path.GetFileName(imageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "stocks");
        var mainPath = Path.Combine(uploadsFolder, fileName);
        if (!File.Exists(mainPath))
        {
            return null; // Dosya yok — atla.
        }

        var thumbsFolder = Path.Combine(uploadsFolder, "thumbs");
        Directory.CreateDirectory(thumbsFolder);
        var thumbPath = Path.Combine(thumbsFolder, fileName);

        using var image = await Image.LoadAsync(mainPath, cancellationToken);
        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;

        // Ana görseli yerinde küçült: geçici dosyaya yaz, sonra üzerine taşı (yarım yazımı önler).
        ResizeToMax(image, MainMaxDimension);
        var tempPath = Path.Combine(uploadsFolder, $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
        await image.SaveAsync(tempPath, cancellationToken);
        File.Move(tempPath, mainPath, overwrite: true);

        // Thumbnail üret.
        ResizeToMax(image, ThumbMaxDimension);
        await image.SaveAsync(thumbPath, cancellationToken);

        return $"/uploads/stocks/thumbs/{fileName}";
    }

    public void Delete(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/stocks/", StringComparison.Ordinal))
        {
            return;
        }

        DeletePhysical(imageUrl);

        // Aynı isimli thumbnail'i de sil (varsa).
        var fileName = Path.GetFileName(imageUrl);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            DeletePhysical($"/uploads/stocks/thumbs/{fileName}");
        }
    }

    private void DeletePhysical(string relativeUrl)
    {
        var physicalPath = Path.Combine(
            _environment.WebRootPath,
            relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}
