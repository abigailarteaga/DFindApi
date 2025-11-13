namespace DFindApi.Models
{
    public class LoginRequest
    {
        public string Correo { get; set; } = null!;
        public string ContrasenaHash { get; set; } = null!;
    }
}
