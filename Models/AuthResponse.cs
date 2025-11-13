namespace DFindApi.Models
{
    public class AuthResponse
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? RutaImagenPerfil { get; set; }
        public decimal TamanoFuente { get; set; }
        public bool ModoOscuro { get; set; }
        public bool NotificacionesSonido { get; set; }
        public bool NotificacionesVibracion { get; set; }
        public bool AceptoTerminos { get; set; }
        public DateTime? FechaAceptoTerminos { get; set; }
        public string? VersionTerminos { get; set; }
        public string? IpAceptacion { get; set; }
        public DateTime CreadoEl { get; set; }
        public DateTime ActualizadoEl { get; set; }
    }
}
