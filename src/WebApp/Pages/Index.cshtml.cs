using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace eShop.WebApp.Pages;

[OutputCache(PolicyName = "CatalogPolicy")]
public class IndexModel(CatalogService catalogService) : PageModel
{
    public const int PageSize = 9;

    [BindProperty(SupportsGet = true)]
    public int? PageNum { get; set; }

    [BindProperty(SupportsGet = true, Name = "brand")]
    public int? BrandId { get; set; }

    [BindProperty(SupportsGet = true, Name = "type")]
    public int? ItemTypeId { get; set; }

    public List<CatalogItem> CatalogItems { get; private set; } = [];
    public int TotalCount { get; private set; }
    public IEnumerable<CatalogBrand> Brands { get; private set; } = [];
    public IEnumerable<CatalogItemType> ItemTypes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var pageIndex = (PageNum.GetValueOrDefault(1) - 1);
        var brandsTask = catalogService.GetBrands();
        var typesTask = catalogService.GetTypes();
        var catalogTask = catalogService.GetCatalogItems(pageIndex, PageSize, BrandId, ItemTypeId);

        await Task.WhenAll(brandsTask, typesTask, catalogTask);

        Brands = brandsTask.Result;
        ItemTypes = typesTask.Result;
        var result = catalogTask.Result;
        CatalogItems = result.Data;
        TotalCount = result.Count;
    }

    public async Task<IActionResult> OnGetPartialAsync()
    {
        await OnGetAsync();
        return Partial("_CatalogItems", new CatalogItemsViewModel
        {
            Items = CatalogItems,
            TotalCount = TotalCount,
            PageSize = PageSize,
            CurrentPage = PageNum ?? 1,
            BrandId = BrandId,
            ItemTypeId = ItemTypeId
        });
    }
}

public class CatalogItemsViewModel
{
    public List<CatalogItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int? BrandId { get; set; }
    public int? ItemTypeId { get; set; }
}
