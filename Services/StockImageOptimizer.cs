using Microsoft.EntityFrameworkCore;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Services;

public record StockImageOptimizeSummary(int Total, int Optimized, int Skipped, int Failed);

/// <summary>Daha önce yüklenmiş (thumbnail'i olmayan) ürün görsellerini toplu küçültür.</summary>
public class StockImageOptimizer
{
    private readonly ApplicationDbContext _context;
    private readonly IStockImageStorage _storage;

    public StockImageOptimizer(ApplicationDbContext context, IStockImageStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<StockImageOptimizeSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        // Sadece görseli olan ve henüz thumbnail üretilmemiş kayıtlar (yeniden çalıştırılabilir/idempotent).
        var items = await _context.StockItems
            .Where(s => s.ImageUrl != null && s.ImageUrl != "" && s.ThumbnailUrl == null)
            .ToListAsync(cancellationToken);

        int optimized = 0, skipped = 0, failed = 0;

        foreach (var item in items)
        {
            try
            {
                var thumbnailUrl = await _storage.OptimizeExistingAsync(item.ImageUrl, cancellationToken);
                if (thumbnailUrl is null)
                {
                    skipped++; // Dosya bulunamadı vb.
                    continue;
                }

                item.ThumbnailUrl = thumbnailUrl;
                await _context.SaveChangesAsync(cancellationToken); // İlerlemeyi kayıt bazında sakla.
                optimized++;
            }
            catch
            {
                failed++;
            }
        }

        return new StockImageOptimizeSummary(items.Count, optimized, skipped, failed);
    }
}
