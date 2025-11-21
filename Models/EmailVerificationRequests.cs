namespace DFindApi.Models
{
    public class EnviarCodigoVerificacionRequest
    {
        public string Correo { get; set; } = null!;
    }

    public class VerificarCodigoRequest
    {
        public string Correo { get; set; } = null!;
        public string Codigo { get; set; } = null!;
    }
}
