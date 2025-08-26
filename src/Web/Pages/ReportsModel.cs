using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;



public class ReportsModel : BasePageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ReportsModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IActionResult> OnGetAsync(string handler = null, string type = null, string format = null)
    {
        var fromDate = DateTime.Today.AddDays(-30);
        var toDate = DateTime.Today;
        
        if (!string.IsNullOrEmpty(handler))
        {
            if (handler == "Generate" || handler == "Export")
            {
                if (type == "daily-collection")
                    return await OnPostCollectionsReportAsync(fromDate, toDate, format ?? "pdf");
                else if (type == "pos-sales")
                    return await OnPostSalesReportAsync(fromDate, toDate, format ?? "pdf");
                else
                    return await GenerateMockReport(type ?? "unknown", format ?? "pdf", fromDate, toDate);
            }
            else if (handler == "ExportAll")
            {
                return await GenerateMockReport("all-reports", "pdf", fromDate, toDate);
            }
            else if (handler == "LedgerStatement")
            {
                var farmerId = Request.Query["farmerId"].ToString();
                return await GenerateLedgerStatement(farmerId, fromDate, toDate, format ?? "pdf");
            }
        }
        
        return Page();
    }

    private async Task<IActionResult> GenerateMockReport(string type, string format, DateTime fromDate, DateTime toDate)
    {
        // Generate mock data for reports that don't have real data
        var mockData = new List<dynamic>
        {
            new { name = "Sample Data", value = 100, date = DateTime.Today },
            new { name = "Mock Entry", value = 200, date = DateTime.Today.AddDays(-1) }
        };

        if (format == "excel")
            return ExportMockToExcel(mockData, type, fromDate, toDate);
        else
            return ExportMockToPdf(mockData, type, fromDate, toDate);
    }

    public async Task<IActionResult> OnPostCollectionsReportAsync(DateTime fromDate, DateTime toDate, string format)
    {
        using var connection = GetConnection();
        var data = await connection.QueryAsync<dynamic>(
            "SELECT f.name as farmer_name, mc.qty_ltr as quantity, mc.fat_pct as fat_percentage, mc.price_per_ltr as rate_per_liter, mc.due_amt as total_amount, mc.date as collection_date FROM dairy.milk_collection mc JOIN dairy.farmer f ON mc.farmer_id = f.id WHERE mc.date BETWEEN @from AND @to ORDER BY mc.date",
            new { from = fromDate, to = toDate.AddDays(1) });

        if (format == "excel")
            return ExportCollectionsToExcel(data, fromDate, toDate);
        else
            return ExportCollectionsToPdf(data, fromDate, toDate);
    }

    public async Task<IActionResult> OnPostSalesReportAsync(DateTime fromDate, DateTime toDate, string format)
    {
        using var connection = GetConnection();
        var data = await connection.QueryAsync<dynamic>(
            "SELECT c.name as customer_name, s.qty_ltr as quantity, s.unit_price as rate_per_liter, s.paid_amt as total_amount, s.date as sale_date FROM dairy.sales s JOIN dairy.customer c ON s.customer_id = c.id WHERE s.date BETWEEN @from AND @to ORDER BY s.date",
            new { from = fromDate, to = toDate.AddDays(1) });

        if (format == "excel")
            return ExportSalesToExcel(data, fromDate, toDate);
        else
            return ExportSalesToPdf(data, fromDate, toDate);
    }

    private IActionResult ExportCollectionsToExcel(IEnumerable<dynamic> data, DateTime fromDate, DateTime toDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Collections");
        
        worksheet.Cell(1, 1).Value = "Farmer Name";
        worksheet.Cell(1, 2).Value = "Quantity";
        worksheet.Cell(1, 3).Value = "Fat %";
        worksheet.Cell(1, 4).Value = "Rate";
        worksheet.Cell(1, 5).Value = "Amount";
        worksheet.Cell(1, 6).Value = "Date";
        
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.farmer_name;
            worksheet.Cell(row, 2).Value = item.quantity;
            worksheet.Cell(row, 3).Value = item.fat_percentage;
            worksheet.Cell(row, 4).Value = item.rate_per_liter;
            worksheet.Cell(row, 5).Value = item.total_amount;
            worksheet.Cell(row, 6).Value = ((DateTime)item.collection_date).ToString("dd/MM/yyyy");
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"Collections_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx");
    }

    private IActionResult ExportSalesToExcel(IEnumerable<dynamic> data, DateTime fromDate, DateTime toDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sales");
        
        worksheet.Cell(1, 1).Value = "Customer Name";
        worksheet.Cell(1, 2).Value = "Quantity";
        worksheet.Cell(1, 3).Value = "Rate";
        worksheet.Cell(1, 4).Value = "Amount";
        worksheet.Cell(1, 5).Value = "Date";
        
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.customer_name;
            worksheet.Cell(row, 2).Value = item.quantity;
            worksheet.Cell(row, 3).Value = item.rate_per_liter;
            worksheet.Cell(row, 4).Value = item.total_amount;
            worksheet.Cell(row, 5).Value = ((DateTime)item.sale_date).ToString("dd/MM/yyyy");
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"Sales_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx");
    }

    private IActionResult ExportCollectionsToPdf(IEnumerable<dynamic> data, DateTime fromDate, DateTime toDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("DAIRY MANAGEMENT SYSTEM")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().AlignCenter().Text("Village Dairy Cooperative Society")
                        .FontSize(14).SemiBold();
                    column.Item().AlignCenter().Text("Address: Main Road, Village Name, District - 123456")
                        .FontSize(10);
                    column.Item().AlignCenter().Text("Phone: +91-9876543210 | Email: info@dairymanagement.com")
                        .FontSize(10);
                    column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    column.Item().AlignCenter().Text($"Milk Collections Report ({fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy})")
                        .FontSize(16).Bold();
                });
                
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Text("Farmer").Bold();
                        header.Cell().Text("Quantity").Bold();
                        header.Cell().Text("Fat %").Bold();
                        header.Cell().Text("Rate").Bold();
                        header.Cell().Text("Amount").Bold();
                        header.Cell().Text("Date").Bold();
                    });
                    
                    foreach (var item in data)
                    {
                        table.Cell().Text((string)item.farmer_name);
                        table.Cell().Text(((decimal)item.quantity).ToString("F1"));
                        table.Cell().Text(((decimal)item.fat_percentage).ToString("F1"));
                        table.Cell().Text(((decimal)item.rate_per_liter).ToString("F2"));
                        table.Cell().Text(((decimal)item.total_amount).ToString("F2"));
                        table.Cell().Text(((DateTime)item.collection_date).ToString("dd/MM/yyyy"));
                    }
                });
            });
        });
        
        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Collections_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
    }

    private IActionResult ExportSalesToPdf(IEnumerable<dynamic> data, DateTime fromDate, DateTime toDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("DAIRY MANAGEMENT SYSTEM")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().AlignCenter().Text("Village Dairy Cooperative Society")
                        .FontSize(14).SemiBold();
                    column.Item().AlignCenter().Text("Address: Main Road, Village Name, District - 123456")
                        .FontSize(10);
                    column.Item().AlignCenter().Text("Phone: +91-9876543210 | Email: info@dairymanagement.com")
                        .FontSize(10);
                    column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    column.Item().AlignCenter().Text($"Sales Report ({fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy})")
                        .FontSize(16).Bold();
                });
                
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Text("Customer").Bold();
                        header.Cell().Text("Quantity").Bold();
                        header.Cell().Text("Rate").Bold();
                        header.Cell().Text("Amount").Bold();
                        header.Cell().Text("Date").Bold();
                    });
                    
                    foreach (var item in data)
                    {
                        table.Cell().Text((string)item.customer_name);
                        table.Cell().Text(((decimal)item.quantity).ToString("F1"));
                        table.Cell().Text(((decimal)item.rate_per_liter).ToString("F2"));
                        table.Cell().Text(((decimal)item.total_amount).ToString("F2"));
                        table.Cell().Text(((DateTime)item.sale_date).ToString("dd/MM/yyyy"));
                    }
                });
            });
        });
        
        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Sales_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
    }

    private IActionResult ExportMockToExcel(IEnumerable<dynamic> data, string type, DateTime fromDate, DateTime toDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(type);
        
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Value";
        worksheet.Cell(1, 3).Value = "Date";
        
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.name;
            worksheet.Cell(row, 2).Value = item.value;
            worksheet.Cell(row, 3).Value = ((DateTime)item.date).ToString("dd/MM/yyyy");
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"{type}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx");
    }

    private IActionResult ExportMockToPdf(IEnumerable<dynamic> data, string type, DateTime fromDate, DateTime toDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                
                page.Header().Text($"{type} Report ({fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy})")
                    .SemiBold().FontSize(16);
                
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Text("Name").Bold();
                        header.Cell().Text("Value").Bold();
                        header.Cell().Text("Date").Bold();
                    });
                    
                    foreach (var item in data)
                    {
                        string name = item.name?.ToString() ?? "";
                        string value = item.value?.ToString() ?? "";
                        string date = item.date != null ? ((DateTime)item.date).ToString("dd/MM/yyyy") : "";
                        
                        table.Cell().Text(name);
                        table.Cell().Text(value);
                        table.Cell().Text(date);
                    }
                });
            });
        });
        
        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"{type}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
    }

    private async Task<IActionResult> GenerateLedgerStatement(string farmerId, DateTime fromDate, DateTime toDate, string format)
    {
        using var connection = GetConnection();
        
        // Get farmer details
        var farmer = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT name, code, contact, village FROM dairy.farmer WHERE id = @farmerId", 
            new { farmerId });
        
        // Get collections
        var collections = await connection.QueryAsync<dynamic>(@"
            SELECT date, qty_ltr, fat_pct, price_per_ltr, due_amt 
            FROM dairy.milk_collection 
            WHERE farmer_id = @farmerId AND date BETWEEN @fromDate AND @toDate 
            ORDER BY date", 
            new { farmerId, fromDate, toDate });
        
        // Get payments
        var payments = await connection.QueryAsync<dynamic>(@"
            SELECT payment_date as date, amount, payment_method 
            FROM dairy.payment_farmer 
            WHERE farmer_id = @farmerId AND payment_date BETWEEN @fromDate AND @toDate 
            ORDER BY payment_date", 
            new { farmerId, fromDate, toDate });
        
        if (format == "excel")
            return ExportLedgerToExcel(farmer, collections, payments, fromDate, toDate);
        else
            return ExportLedgerToPdf(farmer, collections, payments, fromDate, toDate);
    }

    private IActionResult ExportLedgerToPdf(dynamic farmer, IEnumerable<dynamic> collections, IEnumerable<dynamic> payments, DateTime fromDate, DateTime toDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                
                // Header with company details
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("DAIRY MANAGEMENT SYSTEM")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().AlignCenter().Text("Village Dairy Cooperative Society")
                        .FontSize(14).SemiBold();
                    column.Item().AlignCenter().Text("Address: Main Road, Village Name, District - 123456")
                        .FontSize(10);
                    column.Item().AlignCenter().Text("Phone: +91-9876543210 | Email: info@dairymanagement.com")
                        .FontSize(10);
                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });
                
                page.Content().Column(column =>
                {
                    // Farmer details
                    column.Item().AlignCenter().Text($"LEDGER STATEMENT ({fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy})")
                        .FontSize(16).Bold();
                    
                    column.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            string farmerName = farmer?.name?.ToString() ?? "Unknown";
                            col.Item().Text($"Farmer Name: {farmerName}").Bold();
                            string farmerCode = farmer?.code?.ToString() ?? "N/A";
                            col.Item().Text($"Farmer Code: {farmerCode}");
                        });
                        row.RelativeItem().Column(col =>
                        {
                            string farmerContact = farmer?.contact?.ToString() ?? "N/A";
                            col.Item().Text($"Contact: {farmerContact}");
                            string farmerVillage = farmer?.village?.ToString() ?? "N/A";
                            col.Item().Text($"Village: {farmerVillage}");
                        });
                    });
                    
                    // Collections table
                    column.Item().PaddingTop(20).Text("MILK COLLECTIONS").FontSize(14).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Date").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Quantity (L)").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Fat %").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Rate").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Amount").Bold();
                        });
                        
                        foreach (var item in collections)
                        {
                            table.Cell().Padding(3).Text(((DateTime)item.date).ToString("dd/MM/yyyy"));
                            table.Cell().Padding(3).Text(((decimal)item.qty_ltr).ToString("F1"));
                            table.Cell().Padding(3).Text(((decimal)item.fat_pct).ToString("F1"));
                            table.Cell().Padding(3).Text(((decimal)item.price_per_ltr).ToString("F2"));
                            table.Cell().Padding(3).Text(((decimal)item.due_amt).ToString("F2"));
                        }
                    });
                    
                    // Payments table
                    column.Item().PaddingTop(20).Text("PAYMENTS RECEIVED").FontSize(14).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Date").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Amount").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Method").Bold();
                        });
                        
                        foreach (var payment in payments)
                        {
                            table.Cell().Padding(3).Text(((DateTime)payment.date).ToString("dd/MM/yyyy"));
                            table.Cell().Padding(3).Text(((decimal)payment.amount).ToString("F2"));
                            table.Cell().Padding(3).Text((string)(payment.payment_method?.ToString() ?? "Cash"));
                        }
                    });
                    
                    // Summary
                    var totalCollections = collections.Sum(c => (decimal)c.due_amt);
                    var totalPayments = payments.Sum(p => (decimal)p.amount);
                    var balance = totalCollections - totalPayments;
                    
                    column.Item().PaddingTop(20).AlignRight().Column(col =>
                    {
                        string totalCollectionsText = $"Total Collections: ₹{totalCollections:F2}";
                        string totalPaymentsText = $"Total Payments: ₹{totalPayments:F2}";
                        string balanceText = $"Balance: ₹{balance:F2}";
                        
                        col.Item().Text(totalCollectionsText).Bold();
                        col.Item().Text(totalPaymentsText).Bold();
                        col.Item().Text(balanceText).FontSize(14).Bold()
                            .FontColor(balance >= 0 ? Colors.Green.Medium : Colors.Red.Medium);
                    });
                });
                
                page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        });
        
        var pdfBytes = document.GeneratePdf();
        string farmerNameForFile = farmer?.name?.ToString() ?? "Unknown";
        return File(pdfBytes, "application/pdf", $"Ledger_{farmerNameForFile}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
    }

    private IActionResult ExportLedgerToExcel(dynamic farmer, IEnumerable<dynamic> collections, IEnumerable<dynamic> payments, DateTime fromDate, DateTime toDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ledger Statement");
        
        // Header
        worksheet.Cell(1, 1).Value = "LEDGER STATEMENT";
        worksheet.Range(1, 1, 1, 5).Merge().Style.Font.Bold = true;
        string farmerInfo = $"Farmer: {farmer?.name?.ToString() ?? "Unknown"} ({farmer?.code?.ToString() ?? "N/A"})";
        worksheet.Cell(2, 1).Value = farmerInfo;
        worksheet.Cell(3, 1).Value = $"Period: {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}";
        
        // Collections
        int row = 5;
        worksheet.Cell(row, 1).Value = "COLLECTIONS";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        worksheet.Cell(row, 1).Value = "Date";
        worksheet.Cell(row, 2).Value = "Quantity";
        worksheet.Cell(row, 3).Value = "Fat %";
        worksheet.Cell(row, 4).Value = "Rate";
        worksheet.Cell(row, 5).Value = "Amount";
        worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
        row++;
        
        foreach (var item in collections)
        {
            worksheet.Cell(row, 1).Value = ((DateTime)item.date).ToString("dd/MM/yyyy");
            worksheet.Cell(row, 2).Value = (decimal)item.qty_ltr;
            worksheet.Cell(row, 3).Value = (decimal)item.fat_pct;
            worksheet.Cell(row, 4).Value = (decimal)item.price_per_ltr;
            worksheet.Cell(row, 5).Value = (decimal)item.due_amt;
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        
        string farmerNameForExcel = farmer?.name?.ToString() ?? "Unknown";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"Ledger_{farmerNameForExcel}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx");
    }
}