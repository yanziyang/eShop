using System.Net;
using eShop.WebApp.Services;
using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace eShop.WebApp.Pages;

[OutputCache(PolicyName = "ItemPolicy")]
public class ItemModel(
    CatalogService catalogService,
    BasketState basketState,
    IProductImageUrlProvider productImages,
    IHttpContextAccessor httpContextAccessor) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public CatalogItem? Item { get; private set; }
    public int NumInCart { get; private set; }
    public bool IsLoggedIn { get; private set; }
    public bool ItemNotFound { get; private set; }
    public string ProductImageUrl { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        IsLoggedIn = httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
        try
        {
            Item = await catalogService.GetCatalogItem(Id);
            if (Item != null)
            {
                ProductImageUrl = productImages.GetProductImageUrl(Item);
                if (IsLoggedIn)
                {
                    await UpdateNumInCartAsync();
                }
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            HttpContext.Response.StatusCode = 404;
            ItemNotFound = true;
        }
    }

    public async Task<IActionResult> OnPostAddToCartAsync()
    {
        if (!HttpContext.User.Identity!.IsAuthenticated)
        {
            return Redirect($"/user/login?returnUrl={Uri.EscapeDataString(HttpContext.Request.Path)}");
        }

        Item = await catalogService.GetCatalogItem(Id);
        if (Item is not null)
        {
            await basketState.AddAsync(Item);
            await UpdateNumInCartAsync();
        }

        var basketItems = await basketState.GetBasketItemsAsync();
        var cartCount = basketItems.Sum(i => i.Quantity);

        // If HTMX request, return just the updated cart badge
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Content(cartCount > 0
                ? $"""<span class="cart-badge" id="cart-badge">{cartCount}</span>"""
                : """<span class="cart-badge" id="cart-badge" style="display:none">0</span>""",
                "text/html");
        }

        return RedirectToPage("Item", new { id = Id });
    }

    private async Task UpdateNumInCartAsync()
    {
        var items = await basketState.GetBasketItemsAsync();
        NumInCart = items.FirstOrDefault(row => row.ProductId == Id)?.Quantity ?? 0;
    }
}
