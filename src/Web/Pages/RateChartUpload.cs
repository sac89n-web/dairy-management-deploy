using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class RateChartUploadModel : PageModel
{
    [BindProperty]
    public string MilkType { get; set; } = string.Empty;
    [BindProperty]
    public int ShiftId { get; set; }
    [BindProperty]
    public string EffectiveFrom { get; set; } = string.Empty;
    [BindProperty]
    public string EffectiveTo { get; set; } = string.Empty;
    [BindProperty]
    public new IFormFile? File { get; set; }

    public void OnPost()
    {
        if (File != null)
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var path = $"wwwroot/uploads/ratecharts/{year}/{month}/";
            Directory.CreateDirectory(path);
            var filePath = Path.Combine(path, File.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            File.CopyTo(stream);
            // TODO: Save rate chart info to DB
        }
    }
}
