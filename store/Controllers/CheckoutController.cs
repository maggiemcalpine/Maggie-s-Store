using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Braintree;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartyStreets;
using SmartyStreets.USStreetApi;
using store.Data;
using store.Models;

namespace store.Controllers
{
    public class CheckoutController : Controller
    {
        private ApplicationDbContext _context;
        private IEmailSender _emailSender;
        private IBraintreeGateway _braintreeGateway;
        private IClient<Lookup> _streetClient;

        public CheckoutController(ApplicationDbContext context, IEmailSender emailSender, IBraintreeGateway braintreeGateway, IClient<Lookup> streetClient)
        {
            _context = context;
            _emailSender = emailSender;
            _braintreeGateway = braintreeGateway;
            _streetClient = streetClient;
        }

        public async Task<IActionResult> Index()
        {
            CheckoutViewModel model = new CheckoutViewModel();
            if (User.Identity.IsAuthenticated)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(
                    x => x.UserName == User.Identity.Name);
                model.ContactEmail = user.Email;
                model.FirstName = user.FirstName;
                model.LastName = user.LastName;

                CustomerSearchRequest customerSearchRequest = new CustomerSearchRequest();
                customerSearchRequest.Email.Is(User.Identity.Name);

                var customers = await _braintreeGateway.Customer.SearchAsync(customerSearchRequest);
                if (customers.Ids.Any())
                {
                    Customer customer = customers.FirstItem;

                    model.CreditCards = customer.CreditCards;
                }

            }


            return View(model);
        }
        [HttpPost]
        public IActionResult ValidateAddress([FromBody] Lookup lookup)
        {
            try
            {
                _streetClient.Send(lookup);
                return Json(lookup.Result);
            }
            catch (SmartyException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            if (ModelState.IsValid)
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
                if (cart == null)
                {
                    ModelState.AddModelError("Cart",
                        "There was a problem with your cart, please check your cart to verify that all items are correct");
                }
                else
                {
                    if ((User.Identity.IsAuthenticated) && model.CreditCardSave)
                    {
                        //First, check if the customer exists
                        CustomerSearchRequest customerSearchRequest = new CustomerSearchRequest();
                        customerSearchRequest.Email.Is(User.Identity.Name);

                        Customer customer = null;
                        var customers = await _braintreeGateway.Customer.SearchAsync(customerSearchRequest);
                        if (customers.Ids.Any())
                        {
                            customer = customers.FirstItem;
                        }
                        else
                        {
                            CustomerRequest newCustomer = new CustomerRequest();
                            newCustomer.Email = User.Identity.Name;
                            var createResult = await _braintreeGateway.Customer.CreateAsync(newCustomer);
                            if (createResult.IsSuccess())
                            {
                                customer = createResult.Target;
                            }
                            else
                            {
                                throw new Exception(createResult.Message);
                            }
                        }

                        CreditCardRequest newPaymentMethod = new CreditCardRequest();
                        newPaymentMethod.CustomerId = customer.Id;
                        newPaymentMethod.Number = model.CreditCardNumber;
                        newPaymentMethod.CVV = model.CreditCardVerificationValue;
                        newPaymentMethod.ExpirationMonth = (model.CreditCardExpirationMonth ?? 0).ToString().PadLeft(2, '0');
                        newPaymentMethod.ExpirationYear = (model.CreditCardExpirationYear ?? 0).ToString();

                        var createPaymentResult = await _braintreeGateway.CreditCard.CreateAsync(newPaymentMethod);
                        if (!createPaymentResult.IsSuccess())
                        {
                            throw new Exception(createPaymentResult.Message);
                        }
                        else
                        {
                            model.SavedCreditCardToken = createPaymentResult.Target.Token;
                        }

                    }


                    Lookup lookup = new Lookup();
                    lookup.Street = model.ShippingStreet;
                    lookup.City = model.ShippingCity;
                    lookup.State = model.ShippingState;
                    lookup.ZipCode = model.ShippingPostalCode;
                    _streetClient.Send(lookup);

                    if (lookup.Result.Any())
                    {
                        TransactionRequest braintreeTransaction = new TransactionRequest
                        {
                            Amount = cart.CartItems.Sum(x => x.Quantity * (x.Product.Price ?? 0)),

                        };

                        if (model.SavedCreditCardToken == null)
                        {
                            braintreeTransaction.CreditCard = new TransactionCreditCardRequest
                            {
                                CVV = model.CreditCardVerificationValue,
                                ExpirationMonth = (model.CreditCardExpirationMonth ?? 0).ToString().PadLeft(2, '0'),
                                ExpirationYear = (model.CreditCardExpirationYear ?? 0).ToString(),
                                Number = model.CreditCardNumber     //https://developers.braintreepayments.com/guides/credit-cards/testing-go-live/dotnet
                            };
                        }
                        else
                        {
                            braintreeTransaction.PaymentMethodToken = model.SavedCreditCardToken;
                        }


                        var transactionResult = await _braintreeGateway.Transaction.SaleAsync(braintreeTransaction);
                        if (transactionResult.IsSuccess())
                        {



                            // Take the existing cart, and convert the cart and cart items to an  "order" with "order items"
                            //  - when creating order items, I'm going to "denormalize" the info to copy the price, description, 
                            // etc. of what the customer ordered.
                            StoreOrder order = new StoreOrder
                            {
                                ContactEmail = model.ContactEmail,
                                Created = DateTime.UtcNow,
                                FirstName = model.FirstName,
                                LastModified = DateTime.UtcNow,
                                LastName = model.LastName,
                                ShippingCity = model.ShippingCity,
                                ShippingPostalCode = model.ShippingPostalCode,
                                ShippingState = model.ShippingState,
                                ShippingStreet = model.ShippingStreet,
                                StoreOrderItems = cart.CartItems.Select(x => new StoreOrderItem
                                {
                                    Created = DateTime.UtcNow,
                                    LastModified = DateTime.UtcNow,
                                    ProductDescription = x.Product.Description,
                                    ProductID = x.Product.ID,
                                    ProductName = x.Product.Name,
                                    ProductPrice = x.Product.Price,
                                    Quantity = x.Quantity
                                }).ToHashSet()
                            };

                            _context.StoreOrders.Add(order);
                            // Delete the cart, cart items, and clear the cookie or "user cart" info so that the user 
                            // will get a new cart next time.
                            _context.Carts.Remove(cart);

                            if (User.Identity.IsAuthenticated)
                            {
                                var currentStoreUser = await _context.Users
                                    .Include(x => x.Cart)
                                    .ThenInclude(x => x.CartItems)
                                    .ThenInclude(x => x.Product)
                                    .FirstAsync(x => x.UserName == User.Identity.Name);

                                currentStoreUser.Cart = null;
                            }
                            Response.Cookies.Delete("CartID");

                            await _context.SaveChangesAsync();

                            string subject = "Congratulations, order # " + order.ID + " has been placed";
                            UriBuilder builder = new UriBuilder(
                                Request.Scheme, Request.Host.Host, Request.Host.Port ?? 80, "receipt/index/" + order.ID);
                            string htmlContent = string.Format("<a href=\"{0}\">Check out your order</a>", builder.ToString());
                            await _emailSender.SendEmailAsync(model.ContactEmail, subject, htmlContent);
                            // Redirect to the receipt page
                            return RedirectToAction("Index", "Receipt", new { order.ID });
                        }
                        else
                        {
                            foreach (var transactionError in transactionResult.Errors.All())
                            {
                                this.ModelState.AddModelError(transactionError.Code.ToString(), transactionError.Message);
                            }
                        }
                    }
                }


            }
            return View(model);
        }
    }
}