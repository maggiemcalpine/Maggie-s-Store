using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using store.Data;
using store.Models;

namespace store.Controllers
{
    public class ProductsController : Controller
    {
        private ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private void PopulateEmptyProducts()
        {
            if (_context.Products.Count() == 0)
            {
                _context.Products.AddRange(new Product
                {

                }, new Product
                {

                });
                _context.SaveChanges();
            }
        }


        public IActionResult Index(int? id)
        {
            PopulateEmptyProducts();

            Product product = _context.Products.Find(id);
            if (product != null)
            {
                ProductViewModel model = new ProductViewModel
                {
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price ?? 0m,
                    ImagePath = product.ImageUrl,
                    ID = product.ID

                };
                return View(model);
            }
            return NotFound();
        }

        [HttpPost]
        public IActionResult Index(int id, int quantity) 
        {
            // I have a few cases to work through here : 
            //Does this user have an old cart?  
            //Are they logged in or anonymous?
            Cart cart = null;
            if (User.Identity.IsAuthenticated)
            {
                var currentStoreUser = _context.Users
                                               .Include(x => x.Cart)
                                               .ThenInclude(x => x.CartItems)
                                               .ThenInclude(x => x.Product)
                                               .First(x => x.UserName == User.Identity.Name);
                if (currentStoreUser.Cart != null)
                {
                    cart = currentStoreUser.Cart;
                }
                else
                {
                    cart = new Cart
                    {
                        CookieID = Guid.NewGuid(),
                        Created = DateTime.UtcNow
                    };
                    currentStoreUser.Cart = cart;
                    _context.SaveChanges();
                }
            }

            if ((cart == null) && (Request.Cookies.ContainsKey("CartID")))
            {
                if (Guid.TryParse(Request.Cookies["CartID"], out Guid cookieId))
                {
                    cart = _context.Carts.Include(x => x.CartItems)
                                   .ThenInclude(x => x.Product)
                                   .FirstOrDefault(x => x.CookieID == cookieId);
                }
            }

            if (cart == null)   //I either couldn't find the cart from the cookie, or the user had no cookie.
            {
                cart = new Cart
                {
                    CookieID = Guid.NewGuid(),
                    Created = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
            }
            cart.LastModified = DateTime.UtcNow;

            CartItem cartItem = null;   //I also need to check if this item is already in the cart!
            cartItem = cart.CartItems.FirstOrDefault(x => x.Product.ID == id);

            if (cartItem == null) //If still null, this is the first time this item has been added to the cart
            {
                cartItem = new CartItem
                {
                    Quantity = 0,
                    Product = _context.Products.Find(id),
                    Created = DateTime.UtcNow,
                };
                cart.CartItems.Add(cartItem);
            }
            cartItem.Quantity += quantity;
            cartItem.LastModified = DateTime.UtcNow;
            _context.SaveChanges();
            if (!User.Identity.IsAuthenticated)
            {
                Response.Cookies.Append("CartID", cart.CookieID.Value.ToString());
            }
            return RedirectToAction("Index", "Cart");

        }

        public IActionResult List()
        {
            return View(_context.Products);
        }
    }
}