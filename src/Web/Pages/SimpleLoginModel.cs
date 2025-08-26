using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class SimpleLoginModel : PageModel
{
    public string Username { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string SuccessMessage { get; set; } = "";

    public void OnGet()
    {
        // Check if user is already logged in
        if (HttpContext.Session.GetString("UserId") != null)
        {
            Response.Redirect("/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync(string username, string password)
    {
        Username = username;

        try
        {
            // Simple credential validation
            if (username == "admin" && password == "admin123")
            {
                // Set session
                HttpContext.Session.SetString("UserId", "1");
                HttpContext.Session.SetString("UserName", "Administrator");
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserMobile", "8108891477");
                HttpContext.Session.SetString("DatabaseConnected", "true");
                HttpContext.Session.SetString("ConnectionString", "Host=localhost;Database=postgres;Username=admin;Password=admin123;SearchPath=dairy");

                return RedirectToPage("/Dashboard");
            }
            else
            {
                ErrorMessage = "Invalid username or password. Please try again.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
            return Page();
        }
    }
}