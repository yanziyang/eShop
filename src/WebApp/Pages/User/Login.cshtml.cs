using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShop.WebApp.Pages.User;

[Authorize]
public class LoginModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (!string.IsNullOrEmpty(ReturnUrl))
        {
            var uri = new Uri(ReturnUrl, UriKind.RelativeOrAbsolute);
            return Redirect(uri.IsAbsoluteUri ? "/" : ReturnUrl);
        }
        return Redirect("/");
    }
}
