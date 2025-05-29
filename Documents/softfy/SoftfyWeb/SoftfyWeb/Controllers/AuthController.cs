using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos;
using SoftfyWeb.Dtos;
using SoftfyWeb.Services;
using Microsoft.AspNetCore.Authorization;
using SoftfyWeb.Data;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpGet("protegido")]
        public IActionResult SoloUsuariosLogueados()
        {
            var nombreUsuario = User.Identity?.Name;
            return Ok(new
            {
                mensaje = "¡Hola, estás autenticado!",
                usuario = nombreUsuario
            });
        }

        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto dto)
        {
            if (dto.TipoUsuario != "Artista" && dto.TipoUsuario != "Oyente")
                return BadRequest(new { error = "TipoUsuario inválido. Solo se permite 'Artista' o 'Oyente'." });

            var user = new Usuario
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                TipoUsuario = dto.TipoUsuario
            };

            var resultado = await _userManager.CreateAsync(user, dto.Password);
            if (!resultado.Succeeded)
                return BadRequest(resultado.Errors);

            if (!await _roleManager.RoleExistsAsync(dto.TipoUsuario))
                await _roleManager.CreateAsync(new IdentityRole(dto.TipoUsuario));

            await _userManager.AddToRoleAsync(user, dto.TipoUsuario);

            return Ok(new { mensaje = "Usuario registrado correctamente" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDto dto)
        {
            var resultado = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, false);
            if (!resultado.Succeeded)
                return Unauthorized("Credenciales inválidas");

            var usuario = await _userManager.FindByEmailAsync(dto.Email);
            var roles = await _userManager.GetRolesAsync(usuario);
            var token = _jwtService.GenerarToken(usuario, roles);

            return Ok(new
            {
                mensaje = "Login exitoso",
                token = token
            });
        }

        [HttpPost("registro-artista")]
        public async Task<IActionResult> RegistrarArtista([FromBody] ArtistaRegistroDto dto)
        {
            var user = new Usuario
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                TipoUsuario = "Artista"
            };

            var resultado = await _userManager.CreateAsync(user, dto.Password);
            if (!resultado.Succeeded)
                return BadRequest(resultado.Errors);

            if (!await _roleManager.RoleExistsAsync("Artista"))
                await _roleManager.CreateAsync(new IdentityRole("Artista"));

            await _userManager.AddToRoleAsync(user, "Artista");

            // Crear perfil del artista
            var artista = new Artista
            {
                UsuarioId = user.Id,
                NombreArtistico = dto.NombreArtistico,
                Biografia = dto.Biografia,
                FotoUrl = dto.FotoUrl
            };

            var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            context.Artistas.Add(artista);
            await context.SaveChangesAsync();

            return Ok(new { mensaje = "Artista registrado correctamente" });
        }

    }
}
