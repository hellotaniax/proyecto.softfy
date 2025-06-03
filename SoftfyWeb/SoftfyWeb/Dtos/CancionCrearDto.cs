namespace SoftfyWeb.Dtos
{
    public class CancionCrearDto
    {
        public string Titulo { get; set; }
        public string? Genero { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public string? UrlArchivo { get; set; }
    }
}
