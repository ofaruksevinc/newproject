using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OrderManagementAPI.Core.Models;

namespace OrderManagementAPI.Core.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public string? ShippingAddress { get; set; }
        
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
    
    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
    
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShippingAddress { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    }
    
    public class OrderItemResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
} 