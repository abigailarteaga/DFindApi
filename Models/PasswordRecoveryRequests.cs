namespace DFindApi.Models
{
    public class SolicitarRecuperacionRequest
    {
        public string Correo { get; set; } = null!;
    }

    public class VerificarCodigoRecuperacionRequest
    {
        public string Correo { get; set; } = null!;
        public string Codigo { get; set; } = null!;
    }

    public class RestablecerContrasenaRequest
    {
        public string Correo { get; set; } = null!;
        public string Codigo { get; set; } = null!;
        public string NuevaContrasenaHash { get; set; } = null!;
    }
}
