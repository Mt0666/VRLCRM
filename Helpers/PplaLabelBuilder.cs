using System.Globalization;
using System.Text;

namespace VRLCRM.Helpers;

/// <summary>
/// ARGOX PPLA etiket komutlarını üretir (yazıcıya RAW gönderilir).
/// NOT: Koordinatlar/font/barkod parametreleri ilk fiziksel testten sonra ince ayar gerektirebilir;
/// her alanın parametreleri tek yerde ve açıkça tanımlı, kolayca değiştirilebilir.
/// 203 dpi varsayımı: 4 inç = 812 nokta, 6 inç = 1218 nokta.
/// </summary>
public static class PplaLabelBuilder
{
    private const string STX = "\u0002"; // STX kontrol karakteri (0x02)
    private const string NL = "\r\n";
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Build(string name, string stockCode, string barcodeText, decimal price)
    {
        var sb = new StringBuilder();
        sb.Append(STX).Append('L').Append(NL); // Etiket formatlama moduna gir
        sb.Append("D11").Append(NL);            // Nokta boyutu
        sb.Append("H12").Append(NL);            // Isı / koyuluk (00-20)

        // Ürün adı (büyük)
        sb.Append(TextField(font: '5', hMul: '2', vMul: '2', y: 60, x: 40, Ascii(name))).Append(NL);
        // Stok kodu
        sb.Append(TextField(font: '3', hMul: '1', vMul: '1', y: 200, x: 40, "Stok: " + Ascii(stockCode))).Append(NL);
        // Barkod (Code128, altında okunur numara)
        sb.Append(BarcodeField(y: 300, x: 60, height: 150, Ascii(barcodeText))).Append(NL);
        // Fiyat (çok büyük, altta)
        sb.Append(TextField(font: '5', hMul: '3', vMul: '3', y: 950, x: 40, price.ToString("N2", Tr) + " TL")).Append(NL);

        sb.Append("Q0001").Append(NL); // Adet
        sb.Append('E').Append(NL);     // Bitir ve yazdır
        return sb.ToString();
    }

    /// <summary>Metin alanı: 1=döndürme(0°), font, yatay/dikey çarpan, 0=öznitelik, Y(4), X(4), veri.</summary>
    private static string TextField(char font, char hMul, char vMul, int y, int x, string data) =>
        $"1{font}{hMul}{vMul}0{y:0000}{x:0000}{data}";

    /// <summary>Barkod alanı: 1=döndürme, e=Code128(okunur yazılı), 3=geniş çubuk, 1=dar çubuk, yükseklik(4), Y(4), X(4), veri.</summary>
    private static string BarcodeField(int y, int x, int height, string data) =>
        $"1e31{height:0000}{y:0000}{x:0000}{data}";

    /// <summary>Türkçe karakterleri ASCII'ye çevirir + kontrol karakterlerini atar (yazıcı font/kod sayfası uyumu için).</summary>
    private static string Ascii(string s)
    {
        var map = new Dictionary<char, char>
        {
            ['ş'] = 's', ['Ş'] = 'S', ['ğ'] = 'g', ['Ğ'] = 'G', ['ı'] = 'i', ['İ'] = 'I',
            ['ö'] = 'o', ['Ö'] = 'O', ['ü'] = 'u', ['Ü'] = 'U', ['ç'] = 'c', ['Ç'] = 'C'
        };
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (map.TryGetValue(ch, out var r)) sb.Append(r);
            else if (ch >= 32) sb.Append(ch);
        }
        return sb.ToString();
    }
}
