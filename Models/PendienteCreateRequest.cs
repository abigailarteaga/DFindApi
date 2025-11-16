namespace DFindApi.Models
{
    /// <summary>
    /// Datos para crear un pendiente (item de compra).
    /// </summary>
    public class PendienteCreateRequest
    {
        /// <summary>
        /// Correo del usuario dueño de la lista.
        /// Flutter solo envía este correo.
        /// </summary>
        public string CorreoUsuario { get; set; } = default!;

        /// <summary>
        /// Nombre del producto (ej: Leche, Arroz).
        /// </summary>
        public string Nombre { get; set; } = default!;

        /// <summary>
        /// Lugar donde se compra (ej: Supermaxi, Mercado).
        /// Flutter agrupa por este campo.
        /// </summary>
        public string Lugar { get; set; } = default!;

        /// <summary>
        /// Categoría opcional (Lácteos, Limpieza, etc.)
        /// </summary>
        public string? Categoria { get; set; }

        /// <summary>
        /// Cantidad (por defecto 1).
        /// </summary>
        public int Cantidad { get; set; } = 1;
    }
}
