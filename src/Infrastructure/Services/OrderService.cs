using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagementAPI.Core.DTOs;
using OrderManagementAPI.Core.Interfaces;
using OrderManagementAPI.Core.Models;
using OrderManagementAPI.Infrastructure.Data;

namespace OrderManagementAPI.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        
        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto orderDto)
        {
            // Validate products existence and stock
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;
            
            foreach (var item in orderDto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                
                if (product == null)
                {
                    throw new ArgumentException($"Product with ID {item.ProductId} not found");
                }
                
                if (product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }
                
                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };
                
                orderItems.Add(orderItem);
                totalAmount += product.Price * item.Quantity;
            }
            
            // Create the order
            var order = new Order
            {
                UserId = orderDto.UserId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                ShippingAddress = orderDto.ShippingAddress,
                Status = OrderStatus.Pending,
                OrderItems = orderItems
            };
            
            _context.Orders.Add(order);
            
            // Update stock quantities
            foreach (var item in orderDto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                }
            }
            
            await _context.SaveChangesAsync();
            
            // Map to response DTO
            return await MapOrderToResponseDto(order);
        }
        
        public async Task<List<OrderResponseDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
                
            var orderResponseDtos = new List<OrderResponseDto>();
            
            foreach (var order in orders)
            {
                orderResponseDtos.Add(await MapOrderToResponseDto(order));
            }
            
            return orderResponseDtos;
        }
        
        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .SingleOrDefaultAsync(o => o.Id == id);
                
            if (order == null)
            {
                return null;
            }
            
            return await MapOrderToResponseDto(order);
        }
        
        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .SingleOrDefaultAsync(o => o.Id == id);
                
            if (order == null)
            {
                return false;
            }
            
            // Return items to stock
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }
            
            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        private async Task<OrderResponseDto> MapOrderToResponseDto(Order order)
        {
            var orderItemDtos = new List<OrderItemResponseDto>();
            
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                
                orderItemDtos.Add(new OrderItemResponseDto
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? "Unknown Product",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.UnitPrice * item.Quantity
                });
            }
            
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                Status = order.Status,
                Items = orderItemDtos
            };
        }
    }
} 