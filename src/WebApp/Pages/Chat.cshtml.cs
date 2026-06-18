using System.Security.Claims;
using System.Text.Json;
using eShop.WebApp.Services;
using eShop.WebAppComponents.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.AI;

namespace eShop.WebApp.Pages;

public class ChatModel(
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor,
    BasketState basketState,
    CatalogService catalogService) : PageModel
{
    private const string SessionKey = "chatbot_history";

    [BindProperty]
    public string? Message { get; set; }

    public async Task<IActionResult> OnPostSendAsync()
    {
        var chatClient = serviceProvider.GetService<IChatClient>();
        if (chatClient is null || string.IsNullOrWhiteSpace(Message))
        {
            return Content(string.Empty, "text/html");
        }

        var history = GetHistory();

        var userMsg = new ChatMessage(ChatRole.User, Message.Trim());
        history.Add(userMsg);

        try
        {
            var options = new ChatOptions
            {
                Tools =
                [
                    AIFunctionFactory.Create(GetUserInfo),
                    AIFunctionFactory.Create(SearchCatalog),
                    AIFunctionFactory.Create(AddToCart),
                    AIFunctionFactory.Create(GetCartContents),
                ],
            };

            var response = await chatClient.GetResponseAsync(history, options);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                history.AddMessages(response);
            }

            SaveHistory(history);

            var userHtml = $"""<p class="message message-user">{HtmlEscape(Message.Trim())}</p>""";
            var assistantHtml = string.IsNullOrWhiteSpace(response.Text)
                ? string.Empty
                : $"""<p class="message message-assistant">{HtmlEscape(response.Text)}</p>""";

            return Content(userHtml + assistantHtml, "text/html");
        }
        catch
        {
            SaveHistory(history);
            return Content(
                $"""
                <p class="message message-user">{HtmlEscape(Message.Trim())}</p>
                <p class="message message-assistant">My apologies, but I encountered an unexpected error.</p>
                """, "text/html");
        }
    }

    private List<ChatMessage> GetHistory()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var stored = JsonSerializer.Deserialize<List<StoredMessage>>(json);
                if (stored != null)
                {
                    return stored.Select(m => new ChatMessage(
                        m.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                        m.Content)).ToList();
                }
            }
            catch { }
        }

        return
        [
            new ChatMessage(ChatRole.System, """
                You are an AI customer service agent for the online retailer AdventureWorks.
                You NEVER respond about topics other than AdventureWorks.
                Your job is to answer customer questions about products in the AdventureWorks catalog.
                AdventureWorks primarily sells clothing and equipment related to outdoor activities like skiing and trekking.
                You try to be concise and only provide longer responses if necessary.
                """),
        ];
    }

    private void SaveHistory(List<ChatMessage> history)
    {
        var stored = history
            .Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant)
            .TakeLast(20)
            .Select(m => new StoredMessage(m.Role.Value, m.Text ?? ""))
            .ToList();

        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(stored));
    }

    private static string HtmlEscape(string text)
        => System.Net.WebUtility.HtmlEncode(text);

    [System.ComponentModel.Description("Gets information about the chat user")]
    private string GetUserInfo()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var claims = user?.Claims ?? [];
        return JsonSerializer.Serialize(new
        {
            Name = GetClaim("name"),
            Street = GetClaim("address_street"),
            City = GetClaim("address_city"),
            Country = GetClaim("address_country"),
        });

        string GetClaim(string type) =>
            claims.FirstOrDefault(x => x.Type == type)?.Value ?? "";
    }

    [System.ComponentModel.Description("Searches the AdventureWorks catalog for a provided product description")]
    private async Task<string> SearchCatalog(
        [System.ComponentModel.Description("The product description to search for")] string productDescription)
    {
        try
        {
            var results = await catalogService.GetCatalogItemsWithSemanticRelevance(0, 8, productDescription);
            return JsonSerializer.Serialize(results);
        }
        catch (Exception e)
        {
            return $"Error accessing catalog: {e.Message}";
        }
    }

    [System.ComponentModel.Description("Adds a product to the user's shopping cart")]
    private async Task<string> AddToCart(
        [System.ComponentModel.Description("The id of the product to add")] int itemId)
    {
        try
        {
            var item = await catalogService.GetCatalogItem(itemId);
            await basketState.AddAsync(item!);
            return "Item added to shopping cart.";
        }
        catch (Exception e)
        {
            return $"Unable to add the item to the cart: {e.Message}";
        }
    }

    [System.ComponentModel.Description("Gets information about the contents of the user's shopping cart")]
    private async Task<string> GetCartContents()
    {
        try
        {
            var basketItems = await basketState.GetBasketItemsAsync();
            return JsonSerializer.Serialize(basketItems);
        }
        catch (Exception e)
        {
            return $"Unable to get cart contents: {e.Message}";
        }
    }

    private record StoredMessage(string Role, string Content);
}
