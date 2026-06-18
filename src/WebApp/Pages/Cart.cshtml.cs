using eShop.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShop.WebApp.Pages;

[Authorize]
public class CartModel(BasketState basketState) : PageModel
{
    public IReadOnlyCollection<BasketItem>? BasketItems { get; private set; }
    public decimal? TotalPrice => BasketItems?.Sum(i => i.Quantity * i.UnitPrice);
    public int? TotalQuantity => BasketItems?.Sum(i => i.Quantity);

    public async Task OnGetAsync()
    {
        BasketItems = await basketState.GetBasketItemsAsync();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int quantity)
    {
        await basketState.SetQuantityAsync(productId, quantity);
        BasketItems = await basketState.GetBasketItemsAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_CartItems", BasketItems);
        }

        return RedirectToPage();
    }
}
