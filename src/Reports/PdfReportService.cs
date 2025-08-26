using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;

namespace Dairy.Reports
{
    public class PdfReportService
    {
    public byte[] GenerateReport(IEnumerable<ReportRow> rows, string systemName, string contact, string address)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text(systemName).FontSize(20).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Type");
                            header.Cell().Text("Date");
                            header.Cell().Text("Branch");
                            header.Cell().Text("Shift");
                            header.Cell().Text("Amount");
                        });
                        foreach (var r in rows)
                        {
                            table.Cell().Text(r.Type);
                            table.Cell().Text(r.Date);
                            table.Cell().Text(r.BranchId.ToString());
                            table.Cell().Text(r.ShiftId.ToString());
                            table.Cell().Text(r.Amount.ToString("F2"));
                        }
                    });
                    page.Footer().Text($"Contact: {contact} | Address: {address}").FontSize(10);
                });
            });
            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }
    }
}
