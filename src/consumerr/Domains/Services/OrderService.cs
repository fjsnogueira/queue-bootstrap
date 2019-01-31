using System;
using System.Threading.Tasks;
using Consumer.Domains.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.Domains.Services
{
    public interface IOrderService
    {
        Task<Order> GetAsync(int id);
        Task UpdateAsync(Order user);
    }

    public class OrderService : IOrderService
    {
        private readonly ISqlService _sqlService;
        private readonly ILogger<OrderService> _logger;
        
        public OrderService(
             ISqlService sqlService,
             ILogger<OrderService> logger) 
        {
            _sqlService = sqlService;
            _logger = logger;
        }

        public Task<Order> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Order user)
        {
            throw new NotImplementedException();
        }
    }
}
