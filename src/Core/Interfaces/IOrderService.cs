using System.Collections.Generic;
using System.Threading.Tasks;
using OrderManagementAPI.Core.DTOs;

namespace OrderManagementAPI.Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto orderDto);
        Task<List<OrderResponseDto>> GetUserOrdersAsync(string userId);
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<bool> DeleteOrderAsync(int id);
    }
} 