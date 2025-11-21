namespace DFindApi.Models
{
    public class PendienteResponse
    {
        public string IdPendiente { get; set; } = default!;
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = default!;
        public string Lugar { get; set; } = default!;
        public string Categoria { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public bool EstaComprado { get; set; }
        public bool Eliminado { get; set; }
        public DateTime CreadoEl { get; set; }
        public DateTime ActualizadoEl { get; set; }
    }
}
