namespace DFindApi.Models
{
    public class RegisterRequest
    {
        public string NombreUsuario { get; set; } = null!;
        public string Correo { get; set; } = null!;
        // IMPORTANTE: aquí ya mandas el hash de la contraseña
        public string ContrasenaHash { get; set; } = null!;
        public bool AceptoTerminos { get; set; } = false;
        public string? VersionTerminos { get; set; }
        public string? IpAceptacion { get; set; }
    }
}
