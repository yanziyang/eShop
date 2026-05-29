using eShop.ServiceDefaults;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());
    options.AddPolicy("CatalogPolicy", policy =>
        policy.SetVaryByQuery("brand", "type", "page")
              .Expire(TimeSpan.FromSeconds(60)));
    options.AddPolicy("ItemPolicy", policy =>
        policy.SetVaryByRouteValue("id")
              .Expire(TimeSpan.FromSeconds(60)));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseOutputCache();
app.UseAntiforgery();

app.MapRazorPages();

app.MapForwarder("/product-images/{id}", "https+http://catalog-api", "/api/catalog/items/{id}/pic");

app.Run();
