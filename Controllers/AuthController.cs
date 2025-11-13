using DFindApi.Data;
using DFindApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DFindApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthRepository _repo;

        public AuthController(AuthRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Registra un nuevo usuario (usa sp_CrearUsuario).
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _repo.RegistrarAsync(request);
                return Ok(user);
            }
            catch (SqlException ex)
            {
                // Por ejemplo, si el correo ya está registrado (RAISERROR en el SP)
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Autentica un usuario (usa sp_AutenticarUsuario).
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _repo.LoginAsync(request);

                if (user == null)
                    return Unauthorized(new { mensaje = "Correo o contraseña inválidos." });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}
