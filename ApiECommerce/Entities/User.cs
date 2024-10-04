using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ApiECommerce.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(150)]
        [Required]
        public string? Email { get; set; }

        [StringLength(100)]
        [Required]
        public string? Password { get; set; }

        [StringLength(100)]
        public string? UrlImage { get; set; }

        [StringLength(80)]
        public string? PhoneNumber { get; set; }

        [NotMapped]
        public IFormFile? Image { get; set; }
        public ICollection<Order>? Orders { get; set; }
    }
}
