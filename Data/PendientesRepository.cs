using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DFindApi.Models;

namespace DFindApi.Data
{
    public class PendientesRepository
    {
        private readonly IConfiguration _config;

        public PendientesRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection()
            => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        // 1) Crear pendiente por correo
        public async Task<PendienteResponse> CreatePendienteForEmailAsync(
            PendienteCreateRequest request)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // Buscar IdUsuario por correo
            int? idUsuario = null;
            using (var cmdUser = new SqlCommand(@"
                SELECT IdUsuario
                FROM Usuarios
                WHERE Correo = @Correo AND Activo = 1;
            ", conn))
            {
                cmdUser.Parameters.AddWithValue("@Correo", request.CorreoUsuario.Trim());
                var result = await cmdUser.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idUsuario = Convert.ToInt32(result);
            }

            if (idUsuario == null)
                throw new InvalidOperationException("Usuario no encontrado para el correo especificado.");

            // Generar IdPendiente
            var idPendiente = $"pend_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            // Crear pendiente con sp_CrearPendiente
            using (var cmd = new SqlCommand("sp_CrearPendiente", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdPendiente", idPendiente);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
                cmd.Parameters.AddWithValue("@Nombre", request.Nombre);
                cmd.Parameters.AddWithValue("@Lugar", request.Lugar);
                cmd.Parameters.AddWithValue("@Categoria", (object?)request.Categoria ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cantidad", request.Cantidad);

                await cmd.ExecuteNonQueryAsync();
            }

            var now = DateTime.UtcNow;

            return new PendienteResponse
            {
                IdPendiente   = idPendiente,
                IdUsuario     = idUsuario.Value,
                Nombre        = request.Nombre,
                Lugar         = request.Lugar,
                Categoria     = request.Categoria ?? string.Empty,
                Cantidad      = request.Cantidad,
                EstaComprado  = false,   // 👈 se crea SIN tachar
                Eliminado     = false,   // 👈 se crea NO eliminado
                CreadoEl      = now,
                ActualizadoEl = now
            };
        }

        // 2) Listar pendientes por correo (solo no eliminados)
        public async Task<List<PendienteResponse>> GetPendientesByEmailAsync(
            string correoUsuario,
            bool? soloComprados = null)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // Buscar IdUsuario
            int? idUsuario = null;
            using (var cmdUser = new SqlCommand(@"
                SELECT IdUsuario
                FROM Usuarios
                WHERE Correo = @Correo AND Activo = 1;
            ", conn))
            {
                cmdUser.Parameters.AddWithValue("@Correo", correoUsuario.Trim());
                var result = await cmdUser.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idUsuario = Convert.ToInt32(result);
            }

            if (idUsuario == null)
                throw new InvalidOperationException("Usuario no encontrado para el correo especificado.");

            using var cmd = new SqlCommand("sp_ObtenerPendientesUsuario", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
            if (soloComprados.HasValue)
                cmd.Parameters.AddWithValue("@SoloComprados", soloComprados.Value);
            else
                cmd.Parameters.AddWithValue("@SoloComprados", DBNull.Value);

            var list = new List<PendienteResponse>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PendienteResponse
                {
                    IdPendiente   = reader.GetString(reader.GetOrdinal("IdPendiente")),
                    IdUsuario     = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                    Nombre        = reader.GetString(reader.GetOrdinal("Nombre")),
                    Lugar         = reader.GetString(reader.GetOrdinal("Lugar")),
                    Categoria     = reader["Categoria"] as string ?? string.Empty,
                    Cantidad      = reader.GetInt32(reader.GetOrdinal("Cantidad")),
                    EstaComprado  = reader.GetBoolean(reader.GetOrdinal("EstaComprado")),
                    Eliminado     = reader.GetBoolean(reader.GetOrdinal("Eliminado")),
                    CreadoEl      = reader.GetDateTime(reader.GetOrdinal("CreadoEl")),
                    ActualizadoEl = reader.GetDateTime(reader.GetOrdinal("ActualizadoEl"))
                });
            }

            // Ordenar por lugar y fecha de creación (ideal para agrupar en Flutter)
            return list
                .Where(p => !p.Eliminado)   // seguridad extra por si el SP no filtró
                .OrderBy(p => p.Lugar)
                .ThenByDescending(p => p.CreadoEl)
                .ToList();
        }

        // 3) Alternar estado comprado (tachado / no tachado)
        public async Task ToggleCompradoAsync(string idPendiente)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_AlternarPendienteComprado", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@IdPendiente", idPendiente);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
                throw new InvalidOperationException("Pendiente no encontrado.");
        }

        public async Task MarkDeletedAsync(string idPendiente)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE Pendientes
                SET Eliminado = 1,
                    ActualizadoEl = SYSUTCDATETIME()
                WHERE IdPendiente = @IdPendiente;
            ", conn);   // 👈 aquí va el ; y solo un )

            cmd.Parameters.AddWithValue("@IdPendiente", idPendiente);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
                throw new InvalidOperationException("Pendiente no encontrado.");
        }
        // Buscar pendiente por correo + nombre (y opcionalmente lugar) y alternar comprado
        public async Task ToggleCompradoByEmailAndNombreAsync(
            string correoUsuario,
            string nombrePendiente,
            string? lugar = null)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // 1) Buscar IdUsuario
            int? idUsuario = null;
            using (var cmdUser = new SqlCommand(@"
                SELECT IdUsuario
                FROM Usuarios
                WHERE Correo = @Correo AND Activo = 1;
            ", conn))
            {
                cmdUser.Parameters.AddWithValue("@Correo", correoUsuario.Trim());
                var result = await cmdUser.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idUsuario = Convert.ToInt32(result);
            }

            if (idUsuario == null)
                throw new InvalidOperationException("Usuario no encontrado para el correo especificado.");

            // 2) Buscar el IdPendiente según nombre (y opcionalmente lugar)
            string? idPendiente = null;
            using (var cmdPend = new SqlCommand(@"
                SELECT TOP 1 IdPendiente
                FROM Pendientes
                WHERE IdUsuario = @IdUsuario
                AND Eliminado = 0
                AND Nombre = @Nombre
                AND (@Lugar IS NULL OR Lugar = @Lugar)
                ORDER BY CreadoEl DESC;
            ", conn))
            {
                cmdPend.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
                cmdPend.Parameters.AddWithValue("@Nombre", nombrePendiente.Trim());
                cmdPend.Parameters.AddWithValue("@Lugar",
                    (object?) (string.IsNullOrWhiteSpace(lugar) ? null : lugar) ?? DBNull.Value);

                var result = await cmdPend.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idPendiente = (string)result;
            }

            if (idPendiente == null)
                throw new InvalidOperationException("No se encontró un pendiente con ese nombre (y lugar) para este usuario.");

            // 3) Alternar comprado usando el SP existente
            using var cmdToggle = new SqlCommand("sp_AlternarPendienteComprado", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmdToggle.Parameters.AddWithValue("@IdPendiente", idPendiente);

            var rows = await cmdToggle.ExecuteNonQueryAsync();
            if (rows == 0)
                throw new InvalidOperationException("Pendiente no encontrado al intentar alternar estado.");
        }

        public async Task MarkDeletedByEmailAndNombreAsync(
            string correoUsuario,
            string nombrePendiente,
            string? lugar = null)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // 1) Buscar IdUsuario
            int? idUsuario = null;
            using (var cmdUser = new SqlCommand(@"
                SELECT IdUsuario
                FROM Usuarios
                WHERE Correo = @Correo AND Activo = 1;
            ", conn))
            {
                cmdUser.Parameters.AddWithValue("@Correo", correoUsuario.Trim());
                var result = await cmdUser.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idUsuario = Convert.ToInt32(result);
            }

            if (idUsuario == null)
                throw new InvalidOperationException("Usuario no encontrado para el correo especificado.");

            // 2) Buscar el IdPendiente más reciente con ese nombre (y lugar)
            string? idPendiente = null;
            using (var cmdPend = new SqlCommand(@"
                SELECT TOP 1 IdPendiente
                FROM Pendientes
                WHERE IdUsuario = @IdUsuario
                AND Eliminado = 0
                AND Nombre = @Nombre
                AND (@Lugar IS NULL OR Lugar = @Lugar)
                ORDER BY CreadoEl DESC;
            ", conn))
            {
                cmdPend.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
                cmdPend.Parameters.AddWithValue("@Nombre", nombrePendiente.Trim());
                cmdPend.Parameters.AddWithValue("@Lugar",
                    (object?) (string.IsNullOrWhiteSpace(lugar) ? null : lugar) ?? DBNull.Value);

                var result = await cmdPend.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idPendiente = (string)result;
            }

            if (idPendiente == null)
                throw new InvalidOperationException("No se encontró un pendiente con ese nombre (y lugar) para este usuario.");

            // 3) Marcar Eliminado = 1 por IdPendiente (sin ORDER BY)
            using var cmd = new SqlCommand(@"
                UPDATE Pendientes
                SET Eliminado = 1,
                    ActualizadoEl = SYSUTCDATETIME()
                WHERE IdPendiente = @IdPendiente;
            ", conn);

            cmd.Parameters.AddWithValue("@IdPendiente", idPendiente);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
                throw new InvalidOperationException("Pendiente no encontrado al intentar eliminar.");
        }



    }
}
