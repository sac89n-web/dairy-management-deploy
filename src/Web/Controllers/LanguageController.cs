using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

[Route("api/[controller]")]
public class LanguageController : Controller
{
    [HttpPost("set")]
    public IActionResult SetLanguage([FromBody] LanguageRequest request)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(request.Culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );
        
        return Ok(new { success = true });
    }
}

public class LanguageRequest
{
    public string Culture { get; set; } = "";
}