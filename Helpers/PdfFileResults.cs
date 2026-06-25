using Microsoft.AspNetCore.Mvc;

namespace VRLCRM.Helpers;

public static class PdfFileResults
{
    public static FileContentResult AsDownload(byte[] bytes, string fileName) =>
        new(bytes, "application/pdf") { FileDownloadName = fileName };

    public static FileContentResult AsInline(byte[] bytes) =>
        new(bytes, "application/pdf");
}
