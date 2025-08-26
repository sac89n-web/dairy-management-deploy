using Dairy.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public static class ReportEndpoints
{
    [Authorize(Roles = "Admin,CollectionBoy")]
    public static async Task<IResult> Export(
        [FromServices] ExcelReportService excelService,
        [FromServices] PdfReportService pdfService,
        [FromServices] SettingsCache settingsCache,
        [FromQuery] string format = "excel",
        [FromBody] List<ReportRow>? rows = null)
    {
        rows ??= new List<ReportRow>();

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var settings = await settingsCache.GetSettingsAsync();
            var file = pdfService.GenerateReport(rows, settings.SystemName, settings.Contact, settings.Address);
            return Results.File(file, "application/pdf", "report.pdf");
        }

        var excel = excelService.GenerateReport(rows);
        return Results.File(excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "report.xlsx");
    }
}
