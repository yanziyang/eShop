using eShop.WebApp.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShop.WebApp.Pages;

[Authorize]
public class CheckoutModel(
    BasketState basketState,
    IValidator<BasketCheckoutInfo> validator,
    IHttpContextAccessor httpContextAccessor) : PageModel
{
    [BindProperty]
    public BasketCheckoutInfo Info { get; set; } = new();

    public void OnGet()
    {
        PopulateFromClaims();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var cartItems = await basketState.GetBasketItemsAsync();
        if (cartItems.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
            return Page();
        }

        var result = await validator.ValidateAsync(Info);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"Info.{error.PropertyName}", error.ErrorMessage);
            }
            return Page();
        }

        Info.CardTypeId = 1;
        Info.RequestId = Guid.NewGuid();
        await basketState.CheckoutAsync(Info);
        return RedirectToPage("/User/Orders");
    }

    private void PopulateFromClaims()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null) return;

        Info.Street = ReadClaim("address_street");
        Info.City = ReadClaim("address_city");
        Info.State = ReadClaim("address_state");
        Info.Country = ReadClaim("address_country");
        Info.ZipCode = ReadClaim("address_zip_code");

        string? ReadClaim(string type)
            => user.Claims.FirstOrDefault(x => x.Type == type)?.Value;
    }
}
