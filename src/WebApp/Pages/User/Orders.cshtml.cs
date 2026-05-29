using eShop.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShop.WebApp.Pages.User;

[Authorize]
public class OrdersModel(OrderingService orderingService) : PageModel
{
    public OrderRecord[]? Orders { get; private set; }

    public async Task OnGetAsync()
    {
        Orders = await orderingService.GetOrders();
    }

    public async Task<IActionResult> OnGetPollAsync()
    {
        Orders = await orderingService.GetOrders();

        if (!Request.Headers.ContainsKey("HX-Request"))
        {
            return Page();
        }

        var html = string.Concat(Orders.Select(order => $"""
            <li class="orders-item">
                <div class="order-number">{order.OrderNumber}</div>
                <div class="order-date">{order.Date:g}</div>
                <div class="order-total">${order.Total:0.00}</div>
                <div class="order-status"><span class="status {order.Status.ToLower()}">{order.Status}</span></div>
            </li>
            """));

        var list = $"""
            <ul class="orders-list"
                hx-get="/User/Orders?handler=Poll"
                hx-trigger="every 10s"
                hx-target="this"
                hx-swap="outerHTML">
                <li class="orders-header orders-item">
                    <div>Number</div><div>Date</div><div class="total-header">Total</div><div>Status</div>
                </li>
                {html}
            </ul>
            """;

        return Content(list, "text/html");
    }
}
