namespace DFindApi.Models
{
    public class UpdateProfileRequest
    {
        public string Correo { get; set; } = default!;
        public string? NuevoCorreo { get; set; }
        public string? NombreUsuario { get; set; }
        public decimal? TamanoFuente { get; set; }
        public bool? ModoOscuro { get; set; }
        public bool? NotificacionesSonido { get; set; }
        public bool? NotificacionesVibracion { get; set; }
        public string? AvatarTipo { get; set; }
        public string? AvatarClave { get; set; }
    }
}
