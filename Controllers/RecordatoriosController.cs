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

        /// <summary>
        /// Crea un nuevo recordatorio.
        /// </summary>
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



        /// <summary>
        /// Actualiza un recordatorio por título.
        /// </summary>
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

        /// <summary>
        /// Elimina un recordatorio por título.
        /// </summary>
        [HttpDelete("titulo/{titulo}")]
        public async Task<ActionResult> EliminarRecordatorioPorTitulo(string titulo)
        {
            try
            {
                var eliminado = await _repo.EliminarRecordatorioPorTituloAsync(titulo);

                if (!eliminado)
                    return NotFound(new { mensaje = "Recordatorio no encontrado." });

                return Ok(new { mensaje = "Recordatorio eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Cambia el estado activo/inactivo de un recordatorio por título.
        /// </summary>
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

        /// <summary>
        /// Obtiene todos los recordatorios de un usuario por su correo electrónico.
        /// </summary>
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
    }
}