using EShop.Domain;
using EShop.Domain.Domain;
using EShop.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EShop.Repository.Implementation
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<Order> entities;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
            entities = _context.Set<Order>();
        }

        public List<Order> GetAllOrders()
        {
            return entities
                .Include(z => z.ProductsInOrder)
                .Include(z => z.Owner)
                .Include("ProductsInOrder.Product")
                .ToList();
        }

        public Order GetDetailsForOrder(BaseEntity entity)
        {
            return entities
                .Include(z => z.ProductsInOrder)
                .Include(z => z.Owner)
                .Include("ProductsInOrder.Product")
                .SingleOrDefaultAsync(z => z.Id == entity.Id).Result;
        }
    }
}
