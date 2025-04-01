using System;
using System.ComponentModel.DataAnnotations;

namespace OrderManagementAPI.Core.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public int StockQuantity { get; set; }
        
        public string? Description { get; set; }
    }
} 