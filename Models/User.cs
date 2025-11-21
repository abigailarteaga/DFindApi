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
        public string? AvatarTipo { get; set; }  // "preset" o "initial"
        public string? AvatarClave { get; set; } // "avatar1".."avatar5" o "#RRGGBB"
    }
}
