namespace DFindApi.Models
{
    public class User
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContrasenaHash { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string? AvatarTipo { get; set; } 
        public string? AvatarClave { get; set; } 
        public decimal? TamanoFuente { get; set; }
        public bool? ModoOscuro { get; set; }
        public bool? NotificacionesSonido { get; set; }
        public bool? NotificacionesVibracion { get; set; }
    }
}
