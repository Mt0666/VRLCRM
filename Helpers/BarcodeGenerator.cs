using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;

namespace VRLCRM.Helpers;

/// <summary>Etiket için Code128 barkodu üretir (SVG veya PNG).</summary>
public static class BarcodeGenerator
{
    public static string ToCode128Svg(string content, int width = 640, int height = 180)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var writer = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0,
                PureBarcode = true
            }
        };

        return writer.Write(content).Content;
    }

    /// <summary>Code128 barkodunu PNG (siyah/beyaz) olarak üretir — PDF'e gömmek için.</summary>
    public static byte[] ToCode128Png(string content, int width = 600, int height = 170)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<byte>();
        }

        var hints = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.MARGIN, 2 }
        };

        BitMatrix matrix = new MultiFormatWriter().encode(content, BarcodeFormat.CODE_128, width, height, hints);

        using var image = new Image<L8>(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                image[x, y] = matrix[x, y] ? new L8(0) : new L8(255);
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
