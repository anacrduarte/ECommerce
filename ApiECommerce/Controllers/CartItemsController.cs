using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApiECommerce.Context;
using ApiECommerce.Entities;
using System.Security.Claims;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartItemsController : Controller
    {
        private readonly AppDbContext _context;

        public CartItemsController(AppDbContext context)
        {
            _context = context;
        }
        // GET: api/ItensCarrinhoCompra/1
        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user is null)
            {
                return NotFound($"Utilizador com o id = {userId} não encontrado");
            }

            var cartItems = await (from s in _context.CartItems.Where(s => s.ClientId == userId)
                                       join p in _context.Products on s.ProductId equals p.Id
                                       select new
                                       {
                                           Id = s.Id,
                                           Price = s.UnitPrice,
                                          TotalValue = s.TotalValue,
                                           Quantity = s.Quantity,
                                           ProductId = p.Id,
                                           ProductName = p.Name,
                                           UrlImage = p.UrlImage
                                       }).ToListAsync();

            return Ok(cartItems);
        }

        // POST: api/ItensCarrinhoCompra
        // Este método Action trata de uma requisição HTTP do tipo POST para adicionar um
        // novo item ao carrinho de compra ou atualizar a quantidade de um item existente
        // no carrinho. Ele verifica se o item já está no carrinho com base no ID do produto
        // e no ID do cliente. Se o item já estiver no carrinho, sua quantidade é atualizada
        // e o valor total é recalculado. Caso contrário, um novo item é adicionado ao carrinho
        // com as informações fornecidas. Após as operações no banco de dados, o método retorna
        // um código de status 201 (Created).
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CartItem cartItem)
        {
            try
            {
                var shoppingCart = await _context.CartItems.FirstOrDefaultAsync(s =>
                                        s.ProductId == cartItem.ProductId &&
                                        s.ClientId == cartItem.ClientId);

                if (shoppingCart is not null)
                {
                    shoppingCart.Quantity += cartItem.Quantity;
                    shoppingCart.TotalValue = shoppingCart.UnitPrice * shoppingCart.Quantity;
                }
                else
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);

                    var cart = new CartItem()
                    {
                        ClientId = cartItem.ClientId,
                        ProductId = cartItem.ProductId,
                        UnitPrice = cartItem.UnitPrice,
                        Quantity = cartItem.Quantity,
                        TotalValue = (product!.Price) * (cartItem.Quantity)
                    };
                    _context.CartItems.Add(cart);
                }
                await _context.SaveChangesAsync();
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception)
            {
                // Aqui você pode lidar com a exceção, seja registrando-a, enviando uma resposta de erro adequada para o cliente, etc.
                // Por exemplo, você pode retornar uma resposta de erro 500 (Internal Server Error) com uma mensagem genérica para o cliente.
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Ocorreu um erro ao processar a solicitação.");
            }
        }

        // PUT /api/ItensCarrinhoCompra?produtoId = 1 & acao = "aumentar"
        // PUT /api/ItensCarrinhoCompra?produtoId = 1 & acao = "diminuir"
        // PUT /api/ItensCarrinhoCompra?produtoId = 1 & acao = "deletar"
        //--------------------------------------------------------------------
        // Este código manipula itens no carrinho de compras de um usuário com base em uma
        // ação("aumentar", "diminuir" ou "deletar") e um ID de produto.
        // Obtém o usuário logado:
        //    Usa o e-mail do usuário logado para buscar o usuário no banco de dados.
        // Busca o item do carrinho do produto:
        // Procura o item no carrinho com base no ID do produto e no ID do cliente (usuário logado).
        // Realiza a ação especificada:
        //    Aumentar:
        //        Se a quantidade for maior que 0, aumenta a quantidade do item em 1.
        //    Diminuir:
        //        Se a quantidade for maior que 1, diminui a quantidade do item em 1.
        //        Se a quantidade for 1, remove o item do carrinho.
        //    Deletar:
        //        Remove o item do carrinho.
        // Atualiza o valor total do item:
        //    Multiplica o preço unitário pela quantidade, atualizando o valor total do item no carrinho.
        // Salva as alterações no banco de dados:
        //    Salva as alterações feitas no item do carrinho no banco de dados.
        // Retorna o resultado:
        //    Se a ação for bem-sucedida, retorna "Ok".
        //    Se o item não for encontrado, retorna "NotFound".
        //    Se a ação for inválida, retorna "BadRequest".
        /// <summary>
        /// Atualiza a quantidade de um item no carrinho de compras do usuário.
        /// </summary>
        /// <param name="produtoId">O ID do produto.</param>
        /// <param name="acao">A ação a ser realizada no item do carrinho. Opções: 'aumentar', 'diminuir' ou 'deletar'.</param>
        /// <returns>Um objeto IActionResult representando o resultado da operação.</returns>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[HttpPut("{produtoId}/{acao}")]
        public async Task<IActionResult> Put(int productId, string action)
        {
            // Este codigo recupera o endereço de e-mail do usuário autenticado do token JWT decodificado,
            // Claims representa as declarações associadas ao usuário autenticado
            // Assim somente usuários autenticados poderão acessar este endpoint
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user is null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(s =>
                                                   s.ClientId == user!.Id && s.ProductId == productId);

            if (cartItem != null)
            {
                if (action.ToLower() == "aumentar")
                {
                    cartItem.Quantity += 1;
                }
                else if (action.ToLower() == "diminuir")
                {
                    if (cartItem.Quantity > 1)
                    {
                        cartItem.Quantity -= 1;
                    }
                    else
                    {
                        _context.CartItems.Remove(cartItem);
                        await _context.SaveChangesAsync();
                        return Ok();
                    }
                }
                else if (action.ToLower() == "deletar")
                {
                    // Remove o item do carrinho
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                else
                {
                    return BadRequest("Ação Inválida. Use : 'aumentar', 'diminuir', ou 'deletar' para realizar uma ação");
                }

                cartItem.TotalValue = cartItem.UnitPrice* cartItem.Quantity;
                await _context.SaveChangesAsync();
                return Ok($"Operacao : {action} realizada com sucesso");
            }
            else
            {
                return NotFound("Nenhum item encontrado no carrinho");
            }

        }

        //Todo esta codigo é para ter outra visao de como se pode fazer( as tabelas têm nomes diferentes)
        //// PUT /api/ShoppingCartItems?produtoId = 1 & acao = "aumentar"
        //// PUT /api/ShoppingCartItems?produtoId = 1 & acao = "diminuir"
        //// PUT /api/ShoppingCartItems?produtoId = 1 & acao = "apagar"
        //[HttpPut]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> Put(int productId, string action)
        //{
        //    // Este codigo recupera o endereço de e-mail do user autenticado do token JWT decodificado,
        //    // Claims representa as declarações associadas ao user autenticado
        //    // Assim somente os users autenticados poderão aceder a este endpoint
        //    var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        //    if (user is null)
        //    {
        //        return NotFound("Utilizador não encontrado.");
        //    }

        //    var shoppingCartItem = await _appDbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
        //                                           s.ClientId == user!.Id && s.ProductId == productId);

        //    if (shoppingCartItem != null)
        //    {
        //        if (action.ToLower() == "aumentar")
        //        {
        //            shoppingCartItem.Quantity += 1;
        //        }
        //        else if (action.ToLower() == "diminuir")
        //        {
        //            if (shoppingCartItem.Quantity > 1)
        //            {
        //                shoppingCartItem.Quantity -= 1;
        //            }
        //            else
        //            {
        //                _appDbContext.ShoppingCartItems.Remove(shoppingCartItem);
        //                await _appDbContext.SaveChangesAsync();
        //                return Ok();
        //            }
        //        }
        //        else if (action.ToLower() == "apagar")
        //        {
        //            // Remove o item do carrinho
        //            _appDbContext.ShoppingCartItems.Remove(shoppingCartItem);
        //            await _appDbContext.SaveChangesAsync();
        //            return Ok();
        //        }
        //        else
        //        {
        //            return BadRequest("Ação Inválida. Use : 'aumentar', 'diminuir', ou 'apagar' para realizar uma ação");
        //        }

        //        shoppingCartItem.Total = shoppingCartItem.UnitPrice * shoppingCartItem.Quantity;
        //        await _appDbContext.SaveChangesAsync();
        //        return Ok($"Operacao : {action} realizada com sucesso");
        //    }
        //    else
        //    {
        //        return NotFound("Nenhum item encontrado no carrinho");
        //    }
        //}

        /*[Authorize]
        [HttpDelete("{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int productId)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var shoppingCartItem = await _appDbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
            s.ClientId == user.Id && s.ProductId == productId);

            if (shoppingCartItem != null)
            {
                _appDbContext.ShoppingCartItems.Remove(shoppingCartItem);
                await _appDbContext.SaveChangesAsync();
                return Ok("Item removido com sucesso");
            }
            else
            {
                return NotFound("Nenhum item encontrado no carrinho");
            }
        }*/
    }
}
