using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Pedidos/DetalhesPedido/5
        // Retorna os detalhes de um pedido específico, incluindo informações sobre
        // os produtos associados a esse pedido.
        [HttpGet("[action]/{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            var requestDetails = await (from orderDetail in _context.OrderDetails
                                        join request in _context.Orders on orderDetail.OrderId equals request.Id
                                        join product in _context.Products on orderDetail.ProductId equals product.Id
                                        where orderDetail.OrderId == orderId
                                        select new
                                        {
                                            Id = orderDetail.Id,
                                            Quantity = orderDetail.Quantity,
                                            SubTotal = orderDetail.TotalValue,
                                            ProductName = product.Name,
                                            ProductImage = product.UrlImage,
                                            ProductPRice = product.Price,
                                        }).ToListAsync();

            //outra maneira de fazer o mesmo
        //    var orderDetails = await _appDbContext.OrderDetails
        //.Where(od => od.OrderId == orderId)
        //.Include(od => od.Order)
        //.Include(od => od.Product)
        //.Select(od => new
        //{
        //    Id = od.Id,
        //    Quantity = od.Quantity,
        //    SubTotal = od.Total,
        //    ProductName = od.Product!.Name,
        //    ProductImage = od.Product.UrlImage,
        //    Price = od.Product.Price
        //})
        //.ToListAsync();

            if (requestDetails == null || requestDetails.Count == 0)
            {
                return NotFound("Detalhes do pedido não encontrados.");
            }

            return Ok(requestDetails);
        }


        // GET: api/Pedidos/PedidosPorUsuario/5
        // Obtêm todos os pedidos de um usuário específico com base no ID do usuário.
        [HttpGet("[action]/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PedidosPorUsuario(int userId)
        {
            var requests = await (from order in _context.Orders
                                  where order.UserId == userId
                                  orderby order.OrderDate descending
                                  select new
                                  {
                                      Id = order.Id,
                                      TotalValue = order.TotalValue,
                                      OrderDate = order.OrderDate,
                                  }).ToListAsync();


            //outra maneira de fazer o mesmo
          //  var orders = await _appDbContext.Orders
          //.Where(o => o.UserId == userId)
          //.OrderByDescending(o => o.OrderDate)
          //.Select(o => new
          //{
          //    Id = o.Id,
          //    Total = o.Total,
          //    OrderDate = o.OrderDate,
          //})
          //.ToListAsync();

            if (requests is null || requests.Count == 0)
            {
                return NotFound("Não foram encontrados pedidos para o usuário especificado.");
            }

            return Ok(requests);
        }


        //---------------------------------------------------------------------------
        // Neste codigo a criação do pedido, a adição dos detalhes do pedido
        // e a remoção dos itens do carrinho são agrupados dentro de uma transação única.
        // Se alguma operação falhar, a transação será revertida e nenhuma alteração será
        // persistida no banco de dados. Isso garante a consistência dos dados e evita a
        // possibilidade de criar um pedido sem itens no carrinho ou deixar itens
        // no carrinho após criar o pedido.
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] Order order)
        {
            order.OrderDate = DateTime.Now;

            var cartItems = await _context.CartItems
                .Where(cart => cart.ClientId == order.UserId)
                .ToListAsync();

            // Verifica se há itens no carrinho
            if (cartItems.Count == 0)
            {
                return NotFound("Não há itens no carrinho para criar o pedido.");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in cartItems)
                    {
                        var orderDetail = new OrderDetail()
                        {
                            Price = item.UnitPrice,
                            TotalValue = item.TotalValue,
                            Quantity = item.Quantity,
                            ProductId = item.ProductId,
                            OrderId = order.Id,
                        };
                        _context.OrderDetails.Add(orderDetail);
                    }

                    await _context.SaveChangesAsync();
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new { OrderId = order.Id });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Ocorreu um erro ao processar o pedido.");
                }
            }
        }
    }
}
