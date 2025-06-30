using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Data;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using System.Security.Claims;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CancionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CancionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("crear")]
        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> CrearCancion([FromForm] CancionCrearDto dto, IFormFile archivoCancion)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var artista = _context.Artistas.FirstOrDefault(a => a.UsuarioId == usuarioId);
            if (artista == null)
                return BadRequest("El usuario no tiene un perfil de artista.");

            if (archivoCancion == null || archivoCancion.Length == 0)
                return BadRequest("No se ha seleccionado un archivo para la canción.");

            // Validar extensión del archivo
            var allowedExtensions = new[] { ".mp3", ".wav" };
            var fileExtension = Path.GetExtension(archivoCancion.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("El tipo de archivo no es compatible. Solo se permiten archivos .mp3 y .wav.");

            // Ruta donde se guardará el archivo de la canción
            var path = Path.Combine(Directory.GetCurrentDirectory(), "ArchivosCanciones", archivoCancion.FileName);

            // Asegurarse de que el directorio exista
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await archivoCancion.CopyToAsync(stream);
            }

            var cancion = new Cancion
            {
                Titulo = dto.Titulo,
                Genero = dto.Genero,
                FechaLanzamiento = DateTime.SpecifyKind(dto.FechaLanzamiento, DateTimeKind.Utc),
                UrlArchivo = path,  // Ruta donde se guarda el archivo
                ArtistaId = artista.Id
            };

            _context.Canciones.Add(cancion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Canción creada correctamente" });
        }



        [HttpGet("mis-canciones")]
        [Authorize(Roles = "Artista")]
        public IActionResult MisCanciones()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var artista = _context.Artistas.FirstOrDefault(a => a.UsuarioId == usuarioId);
            if (artista == null)
                return NotFound(new { mensaje = "No se encontró el perfil de artista." });

            var canciones = _context.Canciones
                .Where(c => c.ArtistaId == artista.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.Genero,
                    c.FechaLanzamiento,
                    c.UrlArchivo
                })
                .ToList();

            return Ok(canciones);
        }

        [HttpGet("canciones")]
        [AllowAnonymous]
        public IActionResult ObtenerTodasLasCanciones()
        {
            var canciones = _context.Canciones
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.Genero,
                    c.FechaLanzamiento,
                    c.UrlArchivo,
                    Artista = new
                    {
                        c.Artista.NombreArtistico,
                        c.Artista.FotoUrl
                    }
                })
                .ToList();

            return Ok(canciones);
        }


    }
}
