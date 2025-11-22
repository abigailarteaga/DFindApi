using DFindApi.Data;
using DFindApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DFindApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordatoriosController : ControllerBase
    {
        private readonly RecordatoriosRepository _repo;

        public RecordatoriosController(RecordatoriosRepository repo)
        {
            _repo = repo;
        }
        [HttpPost]
        public async Task<ActionResult<RecordatorioResponse>> CrearRecordatorio([FromBody] RecordatorioRequest request)
        {
            try
            {
                var recordatorio = await _repo.CrearRecordatorioAsync(request);
                
                if (recordatorio == null)
                    return BadRequest(new { mensaje = "Error al crear el recordatorio." });

                return CreatedAtAction(nameof(ObtenerRecordatoriosPorCorreo), 
                    new { correo = "creado" }, recordatorio);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
        [HttpPut("titulo/{titulo}")]
        public async Task<ActionResult<RecordatorioResponse>> ActualizarRecordatorioPorTitulo(
            string titulo, 
            [FromBody] ActualizarRecordatorioPorTituloRequest request)
        {
            try
            {
                var recordatorio = await _repo.ActualizarRecordatorioPorTituloAsync(titulo, request);

                if (recordatorio == null)
                    return NotFound(new { mensaje = "Recordatorio no encontrado." });

                return Ok(recordatorio);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
        [HttpDelete("mover-a-papelera")]
        public async Task<IActionResult> EliminarMoviendoAPapeleraPorTitulo(
            [FromBody] MoverRecordatorioAPapeleraRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CorreoUsuario) ||
                string.IsNullOrWhiteSpace(request.Titulo))
            {
                return BadRequest(new { mensaje = "correoUsuario y titulo son obligatorios." });
            }

            try
            {
                var exito = await _repo
                    .EliminarMoviendoAPapeleraPorTituloAsync(
                        request.CorreoUsuario,
                        request.Titulo);

                if (!exito)
                {
                    return NotFound(new { mensaje = "Recordatorio no encontrado." });
                }

                return Ok(new { mensaje = "Recordatorio movido a la papelera correctamente." });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al eliminar el recordatorio.",
                    detalle = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error inesperado al eliminar el recordatorio.",
                    detalle = ex.Message
                });
            }
        }
        [HttpPatch("titulo/{titulo}/toggle-activo")]
        public async Task<ActionResult<RecordatorioResponse>> CambiarEstadoRecordatorioPorTitulo(string titulo)
        {
            try
            {
                var recordatorioActualizado = await _repo.CambiarEstadoPorTituloAsync(titulo);
                
                if (recordatorioActualizado == null)
                    return NotFound(new { mensaje = "Recordatorio no encontrado." });
                
                return Ok(recordatorioActualizado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
        [HttpGet("usuario-por-correo")]
        public async Task<ActionResult<List<RecordatorioResponse>>> ObtenerRecordatoriosPorCorreo([FromQuery] string correo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correo))
                {
                    return BadRequest(new { mensaje = "El correo es requerido." });
                }

                var recordatorios = await _repo.ObtenerRecordatoriosPorCorreoAsync(correo);
                return Ok(recordatorios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
        [HttpPost("restaurar-desde-papelera")]
        public async Task<IActionResult> RestaurarDesdePapelera(
            [FromBody] MoverRecordatorioAPapeleraRequest request)
        {
            try
            {
                var ok = await _repo
                    .RestaurarRecordatorioDesdePapeleraPorTituloAsync(
                        request.CorreoUsuario,
                        request.Titulo);

                if (!ok)
                {
                    return NotFound(new { mensaje = "Elemento en papelera no encontrado." });
                }

                return Ok(new { mensaje = "Recordatorio restaurado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al restaurar el recordatorio.",
                    detalle = ex.Message
                });
            }
        }
    }
}