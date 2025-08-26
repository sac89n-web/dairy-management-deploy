using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult SetLanguage(string culture, string returnUrl = "/")
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
        return LocalRedirect(returnUrl);
    }
}
