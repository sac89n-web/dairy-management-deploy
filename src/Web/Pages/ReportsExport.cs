using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Reports;
using System.Collections.Generic;

public class ReportsExportModel : PageModel
{
    public byte[]? FileBytes { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;

    public void OnPost(string exportType)
    {
        var rows = new List<Dairy.Reports.ReportRow>
        {
            new Dairy.Reports.ReportRow { Type = "Milk Collection", Date = "2025-08-18", BranchId = 1, ShiftId = 1, Amount = 100 }
        };
        if (exportType == "excel")
        {
            var service = new ExcelReportService();
            FileBytes = service.GenerateReport(rows);
            DownloadUrl = "/files/report.xlsx";
            // TODO: Save file to wwwroot/files/report.xlsx
        }
        else if (exportType == "pdf")
        {
            var service = new PdfReportService();
            FileBytes = service.GenerateReport(rows, "Dairy Management System", "9876543210", "123 Dairy Lane");
            DownloadUrl = "/files/report.pdf";
            // TODO: Save file to wwwroot/files/report.pdf
        }
    }
}
