using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var userExists = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (userExists is not null)
            {
                return BadRequest("Já existe utilizador com este email");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u =>
                                     u.Email == user.Email && u.Password == user.Password);

            if (currentUser is null)
            {
                return NotFound("Usuário não encontrado");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
            //var key = _config["JWT:Key"] ?? throw new ArgumentNullException("JWT:Key", "JWT:Key cannot be null.");
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.Email , user.Email)
        };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new ObjectResult(new
            {
                accesstoken = jwt,
                tokentype = "bearer",
                userid = currentUser.Id,
                username = currentUser.Name,
            });
        }

        [Authorize]
        [HttpPost("uploadimage")]
        public async Task<IActionResult> UploadImageUser(IFormFile image)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user is null)
            {
                return NotFound("Usuário não encontrado");
            }

            if (image is not null)
            {
                // Gera um nome de arquivo unico para a imagem enviada 
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                string filePath = Path.Combine("wwwroot/userimages", uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Atualiza a propriedade UrlImagem do usuário com a URL da imagem enviada
                // Assume que a raiz do projeto web é o root
                user.UrlImage = "/userimages/" + uniqueFileName;

                await _context.SaveChangesAsync();
                return Ok("Imagem enviada com sucesso");
            }

            return BadRequest("Nenhuma imagem enviada");
        }

        [Authorize]
        [HttpGet("userimage")]
        public async Task<IActionResult> GetUserProfileImage()
        {
            //verifica se o utilizador esta autenticado
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user is null)
                return NotFound("Usuário não encontrado");

            var profileImage = await _context.Users
                .Where(x => x.Email == userEmail)
                .Select(x => new
                {
                    x.UrlImage,
                })
                .SingleOrDefaultAsync();

            return Ok(profileImage);
        }
    }
}
