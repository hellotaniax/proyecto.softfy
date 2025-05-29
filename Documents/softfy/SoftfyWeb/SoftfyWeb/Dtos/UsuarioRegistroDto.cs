using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Dtos
{
    public class UsuarioRegistroDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido { get; set; }

        [Required]
        [RegularExpression("^(Artista|Oyente)$", ErrorMessage = "Solo se permite 'Artista' o 'Oyente'")]
        public string TipoUsuario { get; set; }
    }
}
