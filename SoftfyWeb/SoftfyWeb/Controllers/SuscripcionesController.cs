﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuscripcionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public SuscripcionesController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Activar suscripción con plan
        [Authorize(Roles = "Oyente")]
        [HttpPost("activar")]
        public async Task<IActionResult> Activar([FromBody] int planId)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var plan = await _context.Planes.FindAsync(planId);
            if (plan == null)
                return BadRequest(new { mensaje = "Plan no válido" });

            if (_context.Suscripciones.Any(s => s.UsuarioPrincipalId == usuario.Id))
                return BadRequest(new { mensaje = "Ya tienes una suscripción activa" });

            var suscripcion = new Suscripcion
            {
                PlanId = plan.Id,
                UsuarioPrincipalId = usuario.Id,
                FechaInicio = DateTime.UtcNow,
                FechaFin = DateTime.UtcNow.AddMonths(1), // Asumiendo que la suscripción es mensual
            };

            _context.Suscripciones.Add(suscripcion);
            await _context.SaveChangesAsync();

            // Agregar como primer miembro
            var miembro = new MiembroSuscripcion
            {
                UsuarioId = usuario.Id,
                SuscripcionId = suscripcion.Id,
                FechaAgregado = DateTime.UtcNow
                
            };

            _context.MiembrosSuscripciones.Add(miembro);
            await _context.SaveChangesAsync();

            // Cambiar rol
            await _userManager.RemoveFromRoleAsync(usuario, "Oyente");
            await _userManager.AddToRoleAsync(usuario, "OyentePremium");
            // Actualizar propiedad personalizada 
            usuario.TipoUsuario = "OyentePremium";
            await _userManager.UpdateAsync(usuario);

            return Ok(new { mensaje = "Suscripción activada correctamente", plan = plan.Nombre });
        }

        // Ver estado de la suscripción
        [Authorize]
        [HttpGet("estado")]
        public async Task<IActionResult> Estado()
        {
            var usuario = await _userManager.GetUserAsync(User);

            var miembro = await _context.MiembrosSuscripciones
                .Include(m => m.Suscripcion)
                    .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);

            if (miembro == null)
                return Ok(new { tipo = "Free" });

            return Ok(new
            {
                tipo = "Premium",
                plan = miembro.Suscripcion.Plan.Nombre,
                precio = miembro.Suscripcion.Plan.Precio,
                inicio = miembro.Suscripcion.FechaInicio,
                fin = miembro.Suscripcion.FechaFin,
                esTitular = miembro.Suscripcion.UsuarioPrincipalId == usuario.Id
            });
        }
        [Authorize]
        [HttpPost("agregar-miembro")]
        public async Task<IActionResult> AgregarMiembro([FromBody] AgregarMiembroDto dto)
        {
            var titular = await _userManager.GetUserAsync(User);

            var suscripcion = await _context.Suscripciones
                .Include(s => s.Plan)
                .Include(s => s.Miembros)
                .FirstOrDefaultAsync(s => s.UsuarioPrincipalId == titular.Id);

            if (suscripcion == null)
                return BadRequest(new { mensaje = "No tienes una suscripción activa" });

            if (suscripcion.Miembros.Count >= suscripcion.Plan.MaxUsuarios)
                return BadRequest(new { mensaje = "Ya has alcanzado el límite de usuarios permitidos para tu plan" });

            var usuarioNuevo = await _userManager.FindByEmailAsync(dto.Email);
            if (usuarioNuevo == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            // Ya es miembro de alguna suscripción
            var yaMiembro = _context.MiembrosSuscripciones.Any(m => m.UsuarioId == usuarioNuevo.Id);
            if (yaMiembro)
                return BadRequest(new { mensaje = "Este usuario ya está en otra suscripción" });

            // Agregar como miembro
            var miembro = new MiembroSuscripcion
            {
                UsuarioId = usuarioNuevo.Id,
                SuscripcionId = suscripcion.Id,
                FechaAgregado = DateTime.UtcNow
            };

            _context.MiembrosSuscripciones.Add(miembro);
            await _context.SaveChangesAsync();

            // Cambiar rol
            await _userManager.RemoveFromRoleAsync(usuarioNuevo, "Oyente");
            await _userManager.AddToRoleAsync(usuarioNuevo, "OyentePremium");
            await _userManager.UpdateSecurityStampAsync(usuarioNuevo);


            return Ok(new { mensaje = "Usuario agregado a la suscripción con éxito" });
        }
        [Authorize]
        [HttpDelete("eliminar-miembro")]
        public async Task<IActionResult> EliminarMiembro([FromBody] EliminarMiembroDto dto)
        {
            var titular = await _userManager.GetUserAsync(User);

            var suscripcion = await _context.Suscripciones
                .Include(s => s.Miembros)
                .FirstOrDefaultAsync(s => s.UsuarioPrincipalId == titular.Id);

            if (suscripcion == null)
                return BadRequest(new { mensaje = "No tienes una suscripción activa" });

            var usuario = await _userManager.FindByEmailAsync(dto.Email);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            if (usuario.Id == titular.Id)
                return BadRequest(new { mensaje = "No puedes eliminarte a ti mismo (titular)" });

            var miembro = await _context.MiembrosSuscripciones
                .FirstOrDefaultAsync(m => m.SuscripcionId == suscripcion.Id && m.UsuarioId == usuario.Id);

            if (miembro == null)
                return BadRequest(new { mensaje = "Este usuario no es miembro de tu suscripción" });

            _context.MiembrosSuscripciones.Remove(miembro);
            await _context.SaveChangesAsync();

            // Cambiar rol a Oyente normal (si no está en otra suscripción)
            var sigueEnOtra = await _context.MiembrosSuscripciones.AnyAsync(m => m.UsuarioId == usuario.Id);
            if (!sigueEnOtra)
            {
                await _userManager.RemoveFromRoleAsync(usuario, "OyentePremium");
                await _userManager.AddToRoleAsync(usuario, "Oyente");
            }

            return Ok(new { mensaje = "Miembro eliminado correctamente" });
        }
        [Authorize(Roles = "OyentePremium")]
        [HttpPost("salir-de-suscripcion")]
        public async Task<IActionResult> SalirDeSuscripcion()
        {
            var usuario = await _userManager.GetUserAsync(User);

            var miembro = await _context.MiembrosSuscripciones
                .Include(m => m.Suscripcion)
                .FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);

            if (miembro == null)
                return BadRequest(new { mensaje = "No perteneces a ninguna suscripción activa" });

            // Verificar si es el titular
            if (miembro.Suscripcion.UsuarioPrincipalId == usuario.Id)
                return BadRequest(new { mensaje = "Eres el titular. Si deseas salir, debes cancelar la suscripción completa." });

            _context.MiembrosSuscripciones.Remove(miembro);
            await _context.SaveChangesAsync();

            // Cambiar el rol
            await _userManager.RemoveFromRoleAsync(usuario, "OyentePremium");
            await _userManager.AddToRoleAsync(usuario, "Oyente");

            return Ok(new { mensaje = "Has salido de la suscripción correctamente" });
        }


    }

}
