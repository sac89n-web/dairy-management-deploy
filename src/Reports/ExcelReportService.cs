using ClosedXML.Excel;
using System.Collections.Generic;
using System.IO;

namespace Dairy.Reports
{
    public class ExcelReportService
    {
        public byte[] GenerateReport(IEnumerable<ReportRow> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Report");
            ws.Cell(1, 1).Value = "Type";
            ws.Cell(1, 2).Value = "Date";
            ws.Cell(1, 3).Value = "Branch";
            ws.Cell(1, 4).Value = "Shift";
            ws.Cell(1, 5).Value = "Amount";
            int row = 2;
            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.Type;
                ws.Cell(row, 2).Value = r.Date;
                ws.Cell(row, 3).Value = r.BranchId;
                ws.Cell(row, 4).Value = r.ShiftId;
                ws.Cell(row, 5).Value = r.Amount;
                row++;
            }
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
