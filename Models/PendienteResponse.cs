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

        /// <summary>
        /// true = comprado (tachado), false = pendiente.
        /// </summary>
        public bool EstaComprado { get; set; }

        /// <summary>
        /// true = eliminado (no se muestra), false = activo.
        /// </summary>
        public bool Eliminado { get; set; }

        public DateTime CreadoEl { get; set; }
        public DateTime ActualizadoEl { get; set; }
    }
}
