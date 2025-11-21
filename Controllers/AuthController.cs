using DFindApi.Data;
using DFindApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using DFindApi.Services;

namespace DFindApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly AuthRepository _repo;
        private readonly EmailService _emailService;

        public AuthController(AuthRepository repo, EmailService emailService)
        {
            _repo = repo;
            _emailService = emailService;
        }

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
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
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
        [HttpPut("profile/by-email")]
        public async Task<ActionResult<AuthResponse>> UpdateProfileByEmail([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var user = await _repo.ActualizarPerfilPorCorreoAsync(request.Correo, request);

                if (user == null)
                    return NotFound(new { mensaje = "Usuario no encontrado." });

                return Ok(user);
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
        [HttpPost("enviar-codigo")]
        public async Task<IActionResult> EnviarCodigoVerificacion([FromBody] EnviarCodigoVerificacionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo))
                return BadRequest(new { mensaje = "El correo es obligatorio." });

            var codigo = await _repo.GenerarYGuardarCodigoVerificacionAsync(request.Correo);

            if (codigo == null)
                return NotFound(new { mensaje = "No se encontró un usuario activo con ese correo." });

            var asunto = "Código de verificación de DFind";

            var cuerpo = $@"
            <!DOCTYPE html>
            <html lang=""es"">
            <head>
            <meta charset=""UTF-8"" />
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
            <title>Código de verificación</title>
            <style>
                * {{
                box-sizing: border-box;
                font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                }}
            </style>
            </head>
            <body style=""margin:0; padding:0; background-color:#f4f4f7;"">
            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f4f4f7; padding: 24px 0;"">
                <tr>
                <td align=""center"">
                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""max-width:480px; background-color:#ffffff; border-radius:16px; box-shadow:0 10px 30px rgba(15,23,42,0.08); overflow:hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg,#4F46E5,#6366F1); padding: 20px 24px; text-align:center;"">
                        <h1 style=""margin:0; font-size:22px; color:#ffffff; font-weight:600;"">
                            Verificación de correo
                        </h1>
                        <p style=""margin:6px 0 0; font-size:13px; color:#E0E7FF;"">
                            Estás a un paso de asegurar tu cuenta en <strong>DFind</strong>.
                        </p>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding: 24px 24px 8px 24px;"">
                        <p style=""margin:0 0 12px 0; font-size:15px; color:#111827;"">
                            Hola
                        </p>
                        <p style=""margin:0 0 12px 0; font-size:14px; line-height:1.6; color:#4B5563;"">
                            Has solicitado un código para <strong>verificar tu correo electrónico</strong> en la app
                            <strong>DFind</strong>. Ingresa el siguiente código en la pantalla de verificación:
                        </p>

                        <!-- Código -->
                        <div style=""margin:20px 0; text-align:center;"">
                            <div style=""display:inline-block; padding:14px 24px; border-radius:999px; background-color:#EEF2FF; border:1px solid #C7D2FE;"">
                            <span style=""font-size:26px; letter-spacing:6px; font-weight:700; color:#4F46E5;"">
                                {codigo}
                            </span>
                            </div>
                        </div>

                        <p style=""margin:0 0 8px 0; font-size:13px; line-height:1.6; color:#6B7280;"">
                            Este código es personal y confidencial. No lo compartas con nadie.
                        </p>
                        <p style=""margin:0 0 8px 0; font-size:13px; line-height:1.6; color:#6B7280;"">
                            El código tiene una validez limitada. Si expira, puedes solicitar uno nuevo desde la app.
                        </p>
                        </td>
                    </tr>

                    <!-- Divider -->
                    <tr>
                        <td style=""padding: 0 24px;"">
                        <hr style=""border:none; border-top:1px solid #E5E7EB; margin: 8px 0 0 0;"" />
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 12px 24px 20px 24px; text-align:left;"">
                        <p style=""margin:0 0 4px 0; font-size:12px; color:#9CA3AF;"">
                            Si tú no solicitaste este código, puedes ignorar este mensaje.
                        </p>
                        <p style=""margin:4px 0 0 0; font-size:11px; color:#9CA3AF;"">
                            Enviado automáticamente por el sistema de DFind · No respondas a este correo.
                        </p>
                        </td>
                    </tr>

                    </table>
                </td>
                </tr>
            </table>
            </body>
            </html>";


            await _emailService.EnviarCorreoAsync(request.Correo, asunto, cuerpo);

            return Ok(new { mensaje = "Código enviado al correo proporcionado." });
        }
                [HttpPost("verificar-codigo")]
        public async Task<IActionResult> VerificarCodigo([FromBody] VerificarCodigoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) ||
                string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest(new { mensaje = "Correo y código son obligatorios." });
            }

            var ok = await _repo.VerificarCodigoAsync(request.Correo, request.Codigo);

            if (!ok)
                return BadRequest(new { mensaje = "Código inválido o expirado." });

            return Ok(new { mensaje = "Correo verificado correctamente." });
        }
        [HttpPost("solicitar-recuperacion")]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitarRecuperacionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo))
            return BadRequest(new { mensaje = "El correo es obligatorio." });

        var codigo = await _repo.GenerarCodigoRecuperacionAsync(request.Correo);

        if (codigo == null)
        {
            return Ok(new { mensaje = "Si el correo está registrado, se ha enviado un código de recuperación." });
        }

        var asunto = "Recuperación de contraseña - DFind";

        var cuerpo = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Recuperar contraseña</title>
  <style>
    * {{
      box-sizing: border-box;
      font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#f4f4f7;"">
  <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f4f4f7; padding: 24px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""max-width:480px; background-color:#ffffff; border-radius:16px; box-shadow:0 10px 30px rgba(15,23,42,0.08); overflow:hidden;"">
          <tr>
            <td style=""background: linear-gradient(135deg,#ec4899,#8b5cf6); padding: 20px 24px; text-align:center;"">
              <h1 style=""margin:0; font-size:22px; color:#ffffff; font-weight:600;"">
                Recuperar contraseña
              </h1>
              <p style=""margin:6px 0 0; font-size:13px; color:#F9A8D4;"">
                Usa este código para restablecer el acceso a tu cuenta.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding: 24px 24px 8px 24px;"">
              <p style=""margin:0 0 12px 0; font-size:14px; line-height:1.6; color:#4B5563;"">
                Has solicitado restablecer tu contraseña en <strong>DFind</strong>.
                Ingresa este código en la pantalla de recuperación:
              </p>
              <div style=""margin:20px 0; text-align:center;"">
                <div style=""display:inline-block; padding:14px 24px; border-radius:999px; background-color:#FEF2F2; border:1px solid #FECACA;"">
                  <span style=""font-size:26px; letter-spacing:6px; font-weight:700; color:#DC2626;"">
                    {codigo}
                  </span>
                </div>
              </div>
              <p style=""margin:0 0 8px 0; font-size:13px; line-height:1.6; color:#6B7280;"">
                Este código es válido solo por unos minutos. Si no fuiste tú, ignora este correo.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding: 12px 24px 20px 24px; text-align:left;"">
              <p style=""margin:0 0 4px 0; font-size:12px; color:#9CA3AF;"">
                Enviado automáticamente por DFind · No respondas a este mensaje.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        await _emailService.EnviarCorreoAsync(request.Correo, asunto, cuerpo);

        return Ok(new { mensaje = "Si el correo está registrado, se ha enviado un código de recuperación." });
    }

    [HttpPost("verificar-codigo-recuperacion")]
    public async Task<IActionResult> VerificarCodigoRecuperacion([FromBody] VerificarCodigoRecuperacionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) ||
            string.IsNullOrWhiteSpace(request.Codigo))
        {
            return BadRequest(new { mensaje = "Correo y código son obligatorios." });
        }

        var ok = await _repo.ValidarCodigoRecuperacionAsync(request.Correo, request.Codigo);

        if (!ok)
            return BadRequest(new { mensaje = "Código de recuperación inválido o expirado." });

        return Ok(new { mensaje = "Código válido. Puedes continuar con el cambio de contraseña." });
    }
    [HttpPost("restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasena([FromBody] RestablecerContrasenaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) ||
            string.IsNullOrWhiteSpace(request.Codigo) ||
            string.IsNullOrWhiteSpace(request.NuevaContrasenaHash))
        {
            return BadRequest(new { mensaje = "Correo, código y nueva contraseña son obligatorios." });
        }

        var ok = await _repo.RestablecerContrasenaAsync(
            request.Correo,
            request.Codigo,
            request.NuevaContrasenaHash
        );

        if (!ok)
            return BadRequest(new { mensaje = "No se pudo restablecer la contraseña. Código inválido o usuario no encontrado." });

        return Ok(new { mensaje = "Contraseña actualizada correctamente." });
    }
    [HttpGet("smtp-check")]
public IActionResult SmtpCheck([FromServices] IConfiguration config)
{
    var smtp = config.GetSection("Smtp");
    return Ok(new
    {
        Host = smtp["Host"],
        Port = smtp["Port"],
        User = smtp["User"],
        EnableSsl = smtp["EnableSsl"],
        FromName = smtp["FromName"]
    });
}


    }
}

