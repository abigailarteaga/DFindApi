namespace DFindApi.Models
{
    public class RecordatorioResponse
    {
        public string IdRecordatorio { get; set; } = null!;
        public int IdUsuario { get; set; }
        public string Titulo { get; set; } = null!;
        public string Descripcion { get; set; }
        public DateTime FechaHora { get; set; }
        public string Prioridad { get; set; } = null!;
        public string Ubicacion { get; set; } = string.Empty;
        public string Objeto { get; set; } = string.Empty;
        public bool EsRepetitivo { get; set; }
        public string FrecuenciaRepeticion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string Color { get; set; } = null!;
        public string? RutaImagen { get; set; }
        public string DiasSeleccionados { get; set; } = string.Empty;
        public DateTime CreadoEl { get; set; }
        public DateTime ActualizadoEl { get; set; }
    }
}
