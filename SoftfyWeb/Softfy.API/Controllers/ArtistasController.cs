using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using System.Linq;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtistasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ArtistasController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ObtenerArtistas()
        {
            var artistas = _context.Artistas
                .Include(a => a.Usuario) // << ESTO AGREGA LA RELACIÓN
                .Select(a => new
                {
                    a.Id,
                    a.NombreArtistico,
                    a.Biografia,
                    a.FotoUrl,
                    UsuarioEmail = a.Usuario.Email
                })
                .ToList();

            return Ok(artistas);
        }
    }
}
