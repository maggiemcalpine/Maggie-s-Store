using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace store.Data
{
    public class ApplicationDbContext : IdentityDbContext<StoreUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<StoreOrder> StoreOrders { get; set; }
        public DbSet<StoreOrderItem> StoreOrderItems { get; set; }
        
    }
    public class StoreUser:Microsoft.AspNetCore.Identity.IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Cart Cart { get; set; }
        public int? CartID { get; set; }
    }
    public class Product
    {
        public Product()
        {
            this.CartItems = new HashSet<CartItem>();
        }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? LastModified { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
    }
    public class Cart
    {
        public Cart()
        {
            this.CartItems = new HashSet<CartItem>();
        }

        public int ID { get; set; }
        public Guid? CookieID { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? LastModified { get; set; }

    }
    public class CartItem
    {
        public int ID { get; set; }
        public Cart Cart { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? LastModified { get; set; }
    }

    public class StoreOrder
    {
        public StoreOrder()
        {
            this.StoreOrderItems = new HashSet<StoreOrderItem>();
        }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid ID { get; set; }
        public string ContactEmail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ShippingStreet { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingPostalCode { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? LastModified { get; set; }

        public ICollection<StoreOrderItem> StoreOrderItems { get; set; }

    }

    public class StoreOrderItem
    {
        public int ID { get; set; }
        public int Quantity { get; set; }
        public int? ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public decimal? ProductPrice { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? LastModified { get; set; }

        public StoreOrder StoreOrder { get; set; }
    }
}
