using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementAPI.Core.Models;
using OrderManagementAPI.Infrastructure.Data;

namespace OrderManagementAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }
        
        // GET: api/Products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            
            if (product == null)
            {
                return NotFound();
            }
            
            return product;
        }
    }
} 