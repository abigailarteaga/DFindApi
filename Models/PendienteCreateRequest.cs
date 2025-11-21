namespace DFindApi.Models
{

    public class PendienteCreateRequest
    {
        public string CorreoUsuario { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public string Lugar { get; set; } = default!;
        public string? Categoria { get; set; }
        public int Cantidad { get; set; } = 1;
    }
}
