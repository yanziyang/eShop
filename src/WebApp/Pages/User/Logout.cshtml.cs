using eShop.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShop.WebApp.Pages.User;

public class LogoutModel(LogOutService logOutService, IHttpContextAccessor httpContextAccessor) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await logOutService.LogOutAsync(httpContextAccessor.HttpContext!);
        return Redirect("/");
    }

    public IActionResult OnGet() => Redirect("/");
}
