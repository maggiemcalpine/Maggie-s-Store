using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using store.Data;


namespace store.Controllers
{
    public class ReceiptController:Controller
    {
        private ApplicationDbContext _context;

        public ReceiptController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(Guid id)
        {
            return View(await _context.StoreOrders
                .Include(x => x.StoreOrderItems)
                .SingleAsync(x => x.ID == id));
        }
    }
}
