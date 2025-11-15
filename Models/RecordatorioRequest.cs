namespace DFindApi.Models
{
    public class RecordatorioRequest
    {
        public string? IdRecordatorio { get; set; }
        public int IdUsuario { get; set; }
        public string Titulo { get; set; } = null!;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Prioridad { get; set; } = "Media";
        public string Ubicacion { get; set; } = string.Empty;
        public string Objeto { get; set; } = string.Empty;
        public bool EsRepetitivo { get; set; } = false;
        public string FrecuenciaRepeticion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public string Color { get; set; } = "blue";
        public string? RutaImagen { get; set; }
        public string DiasSeleccionados { get; set; } = string.Empty;
    }

    public class ActualizarRecordatorioRequest
    {
        public string IdRecordatorio { get; set; } = null!;
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaHora { get; set; }
        public string? Prioridad { get; set; }
        public string? Ubicacion { get; set; }
        public string? Objeto { get; set; }
        public bool? EsRepetitivo { get; set; }
        public string? FrecuenciaRepeticion { get; set; }
        public bool? Activo { get; set; }
        public string? Color { get; set; }
        public string? RutaImagen { get; set; }
        public string? DiasSeleccionados { get; set; }
    }

    public class ActualizarRecordatorioPorTituloRequest
    {
        public string? Descripcion { get; set; }
        public DateTime? FechaHora { get; set; }
        public string? Prioridad { get; set; }
        public string? Ubicacion { get; set; }
        public string? Objeto { get; set; }
        public bool? EsRepetitivo { get; set; }
        public string? FrecuenciaRepeticion { get; set; }
        public bool? Activo { get; set; }
        public string? Color { get; set; }
        public string? RutaImagen { get; set; }
        public string? DiasSeleccionados { get; set; }
    }
}