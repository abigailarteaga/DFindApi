using Microsoft.AspNetCore.Mvc;
using DFindApi.Data;
using DFindApi.Models;

namespace DFindApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PendientesController : ControllerBase
    {
        private readonly PendientesRepository _repo;

        public PendientesController(PendientesRepository repo)
        {
            _repo = repo;
        }

        // POST /api/Pendientes
        // Crea pendiente con EstaComprado = false, Eliminado = false
        [HttpPost]
        public async Task<ActionResult<PendienteResponse>> CrearPendiente(
            [FromBody] PendienteCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.CorreoUsuario))
                return BadRequest("El correo del usuario es obligatorio.");

            try
            {
                var pendiente = await _repo.CreatePendienteForEmailAsync(request);
                return Ok(pendiente);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Usuario no encontrado"))
                    return NotFound("No se encontró un usuario activo con ese correo.");

                throw;
            }
        }

        // GET /api/Pendientes/by-email/{correo}?soloComprados=false
        [HttpGet("by-email/{correoUsuario}")]
        public async Task<ActionResult<List<PendienteResponse>>> GetPendientesPorCorreo(
            string correoUsuario,
            [FromQuery] bool? soloComprados = null)
        {
            try
            {
                var list = await _repo.GetPendientesByEmailAsync(correoUsuario, soloComprados);
                return Ok(list);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Usuario no encontrado"))
                    return NotFound("No se encontró un usuario activo con ese correo.");
                throw;
            }
        }
        // PUT /api/Pendientes/by-email/{correoUsuario}/toggle-comprado-por-nombre?nombre=Leche&lugar=Supermaxi
        [HttpPut("by-email/{correoUsuario}/toggle-comprado-por-nombre")]
        public async Task<IActionResult> ToggleCompradoPorNombre(
            string correoUsuario,
            [FromQuery] string nombre,
            [FromQuery] string? lugar = null)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del pendiente es obligatorio.");

            try
            {
                await _repo.ToggleCompradoByEmailAndNombreAsync(correoUsuario, nombre, lugar);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Usuario no encontrado"))
                    return NotFound("No se encontró un usuario activo con ese correo.");
                if (ex.Message.Contains("No se encontró un pendiente"))
                    return NotFound("No se encontró un pendiente con ese nombre (y lugar) para este usuario.");

                throw;
            }
        }


        // PUT /api/Pendientes/by-email/{correoUsuario}/eliminar-por-nombre?nombre=Leche&lugar=Supermaxi
        [HttpPut("by-email/{correoUsuario}/eliminar-por-nombre")]
        public async Task<IActionResult> EliminarPorNombre(
            string correoUsuario,
            [FromQuery] string nombre,
            [FromQuery] string? lugar = null)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del pendiente es obligatorio.");

            try
            {
                await _repo.MarkDeletedByEmailAndNombreAsync(correoUsuario, nombre, lugar);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Usuario no encontrado"))
                    return NotFound("No se encontró un usuario activo con ese correo.");
                if (ex.Message.Contains("No se encontró un pendiente"))
                    return NotFound("No se encontró un pendiente con ese nombre (y lugar) para este usuario.");

                throw;
            }
        }

    }
}
