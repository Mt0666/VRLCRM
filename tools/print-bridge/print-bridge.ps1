# =====================================================================
# VRLCRM Yazıcı Köprüsü — ARGOX (PPLA) etiket yazıcısı için
# ---------------------------------------------------------------------
# Tarayıcıdan gelen ham PPLA komutlarını yazıcıya RAW olarak gönderir.
# Windows'ta hazır çalışır (PowerShell) — kurulum/derleme GEREKTİRMEZ.
#
# ÇALIŞTIRMA:
#   1) Yazıcının bağlı olduğu bilgisayarda bu klasörü aç.
#   2) PrinterName değerini kendi yazıcı adınla değiştir (aşağıda ya da parametre ile).
#   3) PowerShell'i aç, bu klasöre gel ve şunu çalıştır:
#         powershell -ExecutionPolicy Bypass -File .\print-bridge.ps1
#      (İlk çalıştırmada Windows Güvenlik Duvarı "İzin Ver" sorabilir — Evet de.)
#
#   Yazıcıyı test etmek için (web'e gerek yok):
#         powershell -ExecutionPolicy Bypass -File .\print-bridge.ps1 -Test
#
#   Yüklü yazıcı adlarını görmek için:  Get-Printer | Select Name
# =====================================================================

param(
    [string]$PrinterName = "ARGOX OS-214EX PPLA",
    [int]$Port = 9110,
    [switch]$Test
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class RawPrinterHelper {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public class DOCINFOA {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
    }
    [DllImport("winspool.drv", CharSet=CharSet.Unicode, SetLastError=true)]
    public static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);
    [DllImport("winspool.drv", SetLastError=true)] public static extern bool ClosePrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", CharSet=CharSet.Unicode, SetLastError=true)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);
    [DllImport("winspool.drv", SetLastError=true)] public static extern bool EndDocPrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", SetLastError=true)] public static extern bool StartPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", SetLastError=true)] public static extern bool EndPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.drv", SetLastError=true)]
    public static extern bool WritePrinter(IntPtr hPrinter, byte[] buf, int count, out int written);

    public static string SendBytes(string printerName, byte[] bytes) {
        IntPtr h;
        if (!OpenPrinter(printerName, out h, IntPtr.Zero))
            return "OpenPrinter başarısız — yazıcı adı yanlış olabilir: '" + printerName + "'";
        var di = new DOCINFOA(); di.pDocName = "VRLCRM Etiket"; di.pDataType = "RAW";
        string err = null;
        try {
            if (!StartDocPrinter(h, 1, di)) { err = "StartDocPrinter başarısız"; }
            else {
                StartPagePrinter(h);
                int written;
                if (!WritePrinter(h, bytes, bytes.Length, out written)) err = "WritePrinter başarısız";
                EndPagePrinter(h);
                EndDocPrinter(h);
            }
        } finally { ClosePrinter(h); }
        return err; // null = başarılı
    }
}
"@

function Send-Raw([byte[]]$bytes) {
    return [RawPrinterHelper]::SendBytes($PrinterName, $bytes)
}

# --- Test modu: web'e gerek olmadan basit bir PPLA etiketi bas ---
if ($Test) {
    $stx = [char]2
    $ppla = "${stx}L`r`nD11`r`nH12`r`n1522000600040TEST ETIKET`r`n13110020000401234567890`r`n1e310150030000601234567890`r`nQ0001`r`nE`r`n"
    $bytes = [System.Text.Encoding]::ASCII.GetBytes($ppla)
    $r = Send-Raw $bytes
    if ($r) { Write-Host "HATA: $r" -ForegroundColor Red } else { Write-Host "Test etiketi gönderildi -> $PrinterName" -ForegroundColor Green }
    return
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Prefixes.Add("http://127.0.0.1:$Port/")
try { $listener.Start() } catch {
    Write-Host "Port $Port dinlenemedi. Başka bir uygulama kullanıyor olabilir. Hata: $($_.Exception.Message)" -ForegroundColor Red
    return
}

Write-Host "Yazıcı köprüsü çalışıyor:  http://localhost:$Port/" -ForegroundColor Green
Write-Host "Hedef yazıcı:              $PrinterName"
Write-Host "Durdurmak için: Ctrl+C ya da bu pencereyi kapat."

while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    $req = $ctx.Request
    $res = $ctx.Response
    $res.Headers.Add("Access-Control-Allow-Origin", "*")
    $res.Headers.Add("Access-Control-Allow-Headers", "Content-Type")
    $res.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS")

    try {
        if ($req.HttpMethod -eq "OPTIONS") {
            $res.StatusCode = 204
        }
        elseif ($req.HttpMethod -eq "GET" -and $req.Url.AbsolutePath -eq "/ping") {
            $out = [System.Text.Encoding]::UTF8.GetBytes("ok"); $res.OutputStream.Write($out, 0, $out.Length)
        }
        elseif ($req.HttpMethod -eq "POST" -and $req.Url.AbsolutePath -eq "/print") {
            $ms = New-Object System.IO.MemoryStream
            $req.InputStream.CopyTo($ms)
            # PPLA ASCII/Latin komutları — 1 bayt = 1 karakter olacak şekilde gönder.
            $text = [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
            $bytes = [System.Text.Encoding]::GetEncoding(28591).GetBytes($text) # ISO-8859-1
            $err = Send-Raw $bytes
            if ($err) {
                $res.StatusCode = 500
                Write-Host "Yazdırma hatası: $err" -ForegroundColor Red
                $out = [System.Text.Encoding]::UTF8.GetBytes($err)
            } else {
                $res.StatusCode = 200
                Write-Host "Etiket yazdırıldı." -ForegroundColor Green
                $out = [System.Text.Encoding]::UTF8.GetBytes("printed")
            }
            $res.OutputStream.Write($out, 0, $out.Length)
        }
        else {
            $res.StatusCode = 404
        }
    }
    catch {
        $res.StatusCode = 500
        $out = [System.Text.Encoding]::UTF8.GetBytes($_.Exception.Message)
        $res.OutputStream.Write($out, 0, $out.Length)
    }
    finally {
        $res.OutputStream.Close()
    }
}
