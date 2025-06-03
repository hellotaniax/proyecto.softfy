namespace SoftfyWeb.Dtos
{
    public class ArtistaRegistroDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string NombreArtistico { get; set; }
        public string? Biografia { get; set; }
        public string? FotoUrl { get; set; }
    }
}
