using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderManagementAPI.Core.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        public string? ShippingAddress { get; set; }
        
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
    
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
} 