using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderManagementAPI.Core.DTOs;
using OrderManagementAPI.Core.Interfaces;

namespace OrderManagementAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;
        
        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }
        
        // POST: api/Orders
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            try
            {
                var order = await _orderService.CreateOrderAsync(orderDto);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument when creating order");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation when creating order");
                return BadRequest(new { message = ex.Message });
            }
        }
        
        // GET: api/Orders/user/{userId}
        [HttpGet("user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetUserOrders(string userId)
        {
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }
        
        // GET: api/Orders/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            
            if (order == null)
            {
                return NotFound();
            }
            
            return Ok(order);
        }
        
        // DELETE: api/Orders/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var result = await _orderService.DeleteOrderAsync(id);
            
            if (!result)
            {
                return NotFound();
            }
            
            return NoContent();
        }
    }
} 