namespace VRLCRM.Application.Common;

public static class PhoneNormalizer
{
    /// <summary>
    /// B2B giriş için telefon normalizasyonu (0555, +90, 555 → 10 hane).
    /// </summary>
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        return digits;
    }
}
