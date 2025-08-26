using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

public class ExpensesModel : PageModel
{
    public List<Expense> Expenses { get; set; } = new();

    public void OnGet()
    {
        // TODO: Query from DB
        Expenses.Add(new Expense { CategoryId = 1, BranchId = 1, Amount = 100, Date = "2025-08-18", Notes = "Transport" });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // TODO: Implement delete from database
        return RedirectToPage();
    }
}

public class Expense
{
    public int CategoryId { get; set; }
    public int BranchId { get; set; }
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
