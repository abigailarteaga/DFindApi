using Microsoft.AspNetCore.Mvc;
using DFindApi.Data;
using DFindApi.Models;

namespace DFindApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UsersRepository _repo;
        private readonly AuthRepository _authRepo;

        public UsersController(UsersRepository repo, AuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        [HttpGet]
        public async Task<ActionResult<User>> GetUserByEmail([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return BadRequest("El correo es obligatorio.");

            var user = await _repo.GetByEmailAsync(correo);

            if (user == null)
                return NotFound($"Usuario con correo {correo} no encontrado.");

            return Ok(user);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo))
                return BadRequest("El correo es obligatorio.");

            if (!string.IsNullOrWhiteSpace(request.AvatarTipo))
            {
                var tipo = request.AvatarTipo.Trim().ToLowerInvariant();
                if (tipo != "preset" && tipo != "initial")
                    return BadRequest("AvatarTipo debe ser 'preset' o 'initial'.");

                if (tipo == "preset" &&
                    (request.AvatarClave is null ||
                        !new[] { "avatar1", "avatar2", "avatar3", "avatar4", "avatar5" }
                            .Contains(request.AvatarClave)))
                {
                    return BadRequest("AvatarClave inválido para tipo 'preset'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.NuevoCorreo))
            {
                var verificado = await _authRepo.EstaCorreoPreverificadoAsync(request.NuevoCorreo);
                if (!verificado)
                {
                    return BadRequest("Debes verificar el nuevo correo antes de actualizar el perfil.");
                }
            }

            try
            {
                await _repo.UpdateProfileAsync(request);

                if (!string.IsNullOrWhiteSpace(request.NuevoCorreo))
                {
                    await _authRepo.MarcarVerificacionComoUsadaAsync(request.NuevoCorreo);
                }

                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_DUPLICADO")
            {
                return Conflict("El nuevo correo ya está registrado.");
            }
            catch (InvalidOperationException ex) when (ex.Message == "USUARIO_NO_ENCONTRADO")
            {
                return NotFound("Usuario no encontrado.");
            }
        }
    }
}
