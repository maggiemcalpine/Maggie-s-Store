using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using store.Data;
using Microsoft.EntityFrameworkCore;

namespace store.Controllers
{
    public class CartController : Controller
    {
        private ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            Cart cart = null;
            if (User.Identity.IsAuthenticated)
            {
                var currentStoreUser = await _context.Users
                    .Include(x => x.Cart)
                    .ThenInclude(x => x.CartItems)
                    .ThenInclude(x => x.Product)
                    .FirstAsync(x => x.UserName == User.Identity.Name);

                if (currentStoreUser.Cart != null)
                {
                    cart = currentStoreUser.Cart;
                }
            }
            else if (Request.Cookies.ContainsKey("CartID"))
            {
                if (Guid.TryParse(Request.Cookies["CartID"], out Guid cookieId))
                {
                    cart = await _context.Carts
                        .Include(x => x.CartItems)
                        .ThenInclude(x => x.Product)
                        .FirstOrDefaultAsync(x => x.CookieID == cookieId);
                }
            }

            return View(cart);

        }

        [HttpPost]
        public async Task<IActionResult> Index(Cart model)
        {

            Cart cart = null;
            if (User.Identity.IsAuthenticated)
            {
                var currentStoreUser = await _context.Users
                    .Include(x => x.Cart)
                    .ThenInclude(x => x.CartItems)
                    .ThenInclude(x => x.Product)
                    .FirstAsync(x => x.UserName == User.Identity.Name);
                if (currentStoreUser.Cart != null)
                {
                    cart = currentStoreUser.Cart;
                }
            }
            else if (Request.Cookies.ContainsKey("CartID"))
            {
                if (Guid.TryParse(Request.Cookies["CartID"], out Guid cookieId))
                {
                    cart = await _context.Carts
                        .Include(x => x.CartItems)
                        .ThenInclude(x => x.Product)
                        .FirstOrDefaultAsync(x => x.CookieID == cookieId);
                }
            }

            foreach (var item in cart.CartItems)
            {
                var modelItem = model.CartItems.FirstOrDefault(x => x.ID == item.ID);
                if (modelItem != null && modelItem.Quantity != item.Quantity)
                {
                    item.LastModified = DateTime.UtcNow;
                    item.Quantity = modelItem.Quantity;
                    if (item.Quantity == 0)
                    {
                        _context.CartItems.Remove(item);
                    }
                }
            }
            await _context.SaveChangesAsync();
            return View(cart);
        }
    }

}
