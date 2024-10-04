using EShop.Domain;
using EShop.Domain.Domain;
using EShop.Domain.DTO;
using EShop.Repository.Interface;
using EShop.Service.Interface;
using Org.BouncyCastle.Tls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EShop.Service.Implementation
{
    public class ShoppingCartService : IShoppingCartService
    {

        private readonly IRepository<ShoppingCart> _shoppingCartRepository;
        private readonly IRepository<ProductInShoppingCart> _productInShoppingCartRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<ProductInOrder> _productInOrderRepository;
        private readonly IEmailService _emailService;

        public ShoppingCartService(IRepository<ShoppingCart> shoppingCartRepository, IRepository<ProductInShoppingCart> productInShoppingCartRepository, IUserRepository userRepository, IRepository<Order> orderRepository, IRepository<ProductInOrder> productInOrderRepository, IEmailService emailService)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _productInShoppingCartRepository = productInShoppingCartRepository;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _productInOrderRepository = productInOrderRepository;
            _emailService = emailService;
        }

        public bool AddToShoppingCartConfirmed(ProductInShoppingCart model, string userId)
        {
            var loggedInUser = _userRepository.Get(userId);
            var userShoppingCart = loggedInUser.ShoppingCart;

            if (userShoppingCart.ProductsInShoppingCart == null)
                userShoppingCart.ProductsInShoppingCart = new List<ProductInShoppingCart>();

            userShoppingCart.ProductsInShoppingCart.Add(model);
            _shoppingCartRepository.Update(userShoppingCart);
            return true;

        }

        public bool deleteProductFromnShoppingCart(string userId, Guid productId)
        {
            if(productId != null)
            {
                var loggedInUser = _userRepository.Get(userId);
                var userShoppingCart = loggedInUser.ShoppingCart;
                var product = userShoppingCart.ProductsInShoppingCart.Where(x => x.ProductId == productId).FirstOrDefault();

                userShoppingCart.ProductsInShoppingCart.Remove(product);

                _shoppingCartRepository.Update(userShoppingCart);
                return true;
            }
            return false;
        }

        public ShoppingCartDto getShoppingCartInfo(string userId)
        {
            var loggedInUser = _userRepository.Get(userId);
            var userShoppingcart = loggedInUser.ShoppingCart;
            var allProducts = userShoppingcart?.ProductsInShoppingCart?.ToList();

            var totalPrice = allProducts.Select(x => (x.Product.Price * x.Quantity)).Sum();

            ShoppingCartDto dto = new ShoppingCartDto
            {
                Products = allProducts,
                TotalPrice = totalPrice
            };
            return dto;
        }

        public bool order(string userId)
        {
            if (userId != null)
            {
                var loggedInUser = _userRepository.Get(userId);
                var userShoppingCart = loggedInUser.ShoppingCart;
                EmailMessage message = new EmailMessage();
                message.Subject = "Your order was successfull.";
                message.MailTo = loggedInUser.Email;

                Order order = new Order()
                {
                    Id = Guid.NewGuid(),
                    OwnerId = userId,
                    Owner = loggedInUser
                };

                _orderRepository.Insert(order);
                List<ProductInOrder> productsInOrder = new List<ProductInOrder>();

                var list = userShoppingCart.ProductsInShoppingCart.Select(
                    x => new ProductInOrder()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = x.Product.Id,
                        Product = x.Product,
                        OrderId = order.Id,
                        Order = order,
                        Quantity = x.Quantity,
                    }).ToList();

                StringBuilder sb = new StringBuilder();

                var totalPrice = 0.0;

                sb.AppendLine("Your order is completed. The order conatins: ");

                for (int i = 1; i <= list.Count(); i++)
                {
                    var currentItem = list[i - 1];
                    totalPrice += currentItem.Quantity * currentItem.Product.Price;
                    sb.AppendLine(i.ToString() + ". " + currentItem.Product.ProductName + " with quantity of: " + currentItem.Quantity + " and price of: $" + currentItem.Product.Price);
                }

                sb.AppendLine("Total price for your order: " + totalPrice.ToString());
                message.Body= sb.ToString();

                productsInOrder.AddRange(list);

                foreach (var product in productsInOrder)
                {
                    _productInOrderRepository.Insert(product);
                }

                loggedInUser.ShoppingCart.ProductsInShoppingCart.Clear();
                _userRepository.Update(loggedInUser);
                this._emailService.SendEmailAsync(message);

                return true;
            }
            return false;
        }
    }
}
