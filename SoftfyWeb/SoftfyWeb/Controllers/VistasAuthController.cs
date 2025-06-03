// Controllers/VistasAuthController.cs
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    public class VistasAuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasAuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto dto)
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(dto);
            var response = await client.PostAsync("https://localhost:7130/api/auth/login",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Credenciales inválidas";
                return View(dto);
            }

            var contenido = await response.Content.ReadAsStringAsync();
            var token = JsonDocument.Parse(contenido).RootElement.GetProperty("token").GetString();

            // Guardar el token en una cookie segura
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Asegúrate de estar en HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return RedirectToAction("Bienvenido");
        }


        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioRegistroDto dto)
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(dto);
            var response = await client.PostAsync("https://localhost:7130/api/auth/registro",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error en el registro";
                return View(dto);
            }

            return RedirectToAction("Login");
        }

        public IActionResult Bienvenido()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_token");
            return RedirectToAction("Login");
        }
    }
}
