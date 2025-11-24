using System.Data;
using System.Security.Cryptography;
using DFindApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DFindApi.Data
{
    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new Exception("Connection string no encontrada.");
        }
        public async Task<AuthResponse?> RegistrarAsync(RegisterRequest request)
        {
            int idNuevoUsuario;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CrearUsuario", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@NombreUsuario", request.NombreUsuario);
                cmd.Parameters.AddWithValue("@Correo", request.Correo);
                cmd.Parameters.AddWithValue("@ContrasenaHash", request.ContrasenaHash);
                cmd.Parameters.AddWithValue("@AceptoTerminos", request.AceptoTerminos);
                cmd.Parameters.AddWithValue(
                    "@VersionTerminos",
                    (object?)request.VersionTerminos ?? DBNull.Value
                );
                cmd.Parameters.AddWithValue(
                    "@IpAceptacion",
                    (object?)request.IpAceptacion ?? DBNull.Value
                );

                var idOutput = new SqlParameter("@IdUsuario", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(idOutput);

                await conn.OpenAsync();

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("El correo ya está registrado",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("EMAIL_DUPLICADO");
                    }

                    if (ex.Message.Contains("Debes verificar tu correo",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("CORREO_NO_VERIFICADO");
                    }

                    throw;
                }

                idNuevoUsuario = (int)idOutput.Value;
            }

            var usuario = await ObtenerUsuarioPorCorreoYHashAsync(
                request.Correo,
                request.ContrasenaHash
            );

            return usuario;
        }


        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            return await ObtenerUsuarioPorCorreoYHashAsync(request.Correo, request.ContrasenaHash);
        }
        private async Task<AuthResponse?> ObtenerUsuarioPorCorreoYHashAsync(string correo, string hash)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_AutenticarUsuario", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Correo", correo);
            cmd.Parameters.AddWithValue("@ContrasenaHash", hash);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new AuthResponse
            {
                IdUsuario = (int)reader["IdUsuario"],
                NombreUsuario = reader["NombreUsuario"].ToString() ?? "",
                Correo = reader["Correo"].ToString() ?? "",
                RutaImagenPerfil = reader["RutaImagenPerfil"] as string,
                TamanoFuente = (decimal)reader["TamanoFuente"],
                ModoOscuro = (bool)reader["ModoOscuro"],
                NotificacionesSonido = (bool)reader["NotificacionesSonido"],
                NotificacionesVibracion = (bool)reader["NotificacionesVibracion"],
                AceptoTerminos = (bool)reader["AceptoTerminos"],
                FechaAceptoTerminos = reader["FechaAceptoTerminos"] == DBNull.Value
                    ? null
                    : (DateTime?)reader["FechaAceptoTerminos"],
                VersionTerminos = reader["VersionTerminos"] as string,
                IpAceptacion = reader["IpAceptacion"] as string,
                CreadoEl = (DateTime)reader["CreadoEl"],
                ActualizadoEl = (DateTime)reader["ActualizadoEl"]
            };
        }
        private async Task<AuthResponse?> ObtenerUsuarioPorIdAsync(int idUsuario)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT IdUsuario, NombreUsuario, Correo, RutaImagenPerfil, TamanoFuente,
                    ModoOscuro, NotificacionesSonido, NotificacionesVibracion,
                    AceptoTerminos, FechaAceptoTerminos, VersionTerminos, IpAceptacion,
                    CreadoEl, ActualizadoEl
                FROM Usuarios
                WHERE IdUsuario = @IdUsuario AND Activo = 1;
            ", conn);

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new AuthResponse
            {
                IdUsuario = (int)reader["IdUsuario"],
                NombreUsuario = reader["NombreUsuario"].ToString() ?? "",
                Correo = reader["Correo"].ToString() ?? "",
                RutaImagenPerfil = reader["RutaImagenPerfil"] as string,
                TamanoFuente = (decimal)reader["TamanoFuente"],
                ModoOscuro = (bool)reader["ModoOscuro"],
                NotificacionesSonido = (bool)reader["NotificacionesSonido"],
                NotificacionesVibracion = (bool)reader["NotificacionesVibracion"],
                AceptoTerminos = (bool)reader["AceptoTerminos"],
                FechaAceptoTerminos = reader["FechaAceptoTerminos"] == DBNull.Value
                    ? null
                    : (DateTime?)reader["FechaAceptoTerminos"],
                VersionTerminos = reader["VersionTerminos"] as string,
                IpAceptacion = reader["IpAceptacion"] as string,
                CreadoEl = (DateTime)reader["CreadoEl"],
                ActualizadoEl = (DateTime)reader["ActualizadoEl"]
            };
        }
            private async Task<int?> ObtenerIdUsuarioPorCorreoAsync(string correo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT IdUsuario
                FROM Usuarios
                WHERE Correo = @Correo AND Activo = 1;
            ", conn);

            cmd.Parameters.AddWithValue("@Correo", correo);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            return (int)result;
        }
        public async Task<AuthResponse?> ActualizarPerfilPorCorreoAsync(
            string correoActual,
            UpdateProfileRequest request)
        {
            var idUsuario = await ObtenerIdUsuarioPorCorreoAsync(correoActual);
            if (idUsuario == null)
            {
                return null;
            }

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ActualizarPerfilUsuario", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);

            cmd.Parameters.AddWithValue("@NombreUsuario",
                (object?)request.NombreUsuario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Correo",
                (object?)request.Correo ?? DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return await ObtenerUsuarioPorIdAsync(idUsuario.Value);
        }

    public async Task<string?> GenerarYGuardarCodigoVerificacionAsync(string correo, int minutosValidez = 10)
    {
        var codigo = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using (var cmd = new SqlCommand(@"
            UPDATE Usuarios
            SET CodigoVerificacion = @Codigo,
                CodigoVerificacionExpira = DATEADD(MINUTE, @Minutos, SYSUTCDATETIME())
            WHERE Correo = @Correo
            AND Activo = 1;

            SELECT @@ROWCOUNT;
        ", conn))
        {
            cmd.Parameters.AddWithValue("@Codigo", codigo);
            cmd.Parameters.AddWithValue("@Minutos", minutosValidez);
            cmd.Parameters.AddWithValue("@Correo", correo);

            var rows = (int)await cmd.ExecuteScalarAsync();

            if (rows > 0)
            {
                return codigo;
            }
        }
        using (var cmd2 = new SqlCommand(@"
            MERGE EmailVerifications AS target
            USING (SELECT @Correo AS Correo) AS src
            ON target.Correo = src.Correo
            WHEN MATCHED THEN
                UPDATE SET Codigo = @Codigo,
                        Expira = DATEADD(MINUTE, @Minutos, SYSUTCDATETIME()),
                        FueVerificado = 0,
                        Usado = 0,
                        VerificadoEl = NULL,
                        UsadoEl = NULL
            WHEN NOT MATCHED THEN
                INSERT (Correo, Codigo, Expira, FueVerificado, Usado)
                VALUES (@Correo, @Codigo, DATEADD(MINUTE, @Minutos, SYSUTCDATETIME()), 0, 0);
        ", conn))
        {
            cmd2.Parameters.AddWithValue("@Correo", correo);
            cmd2.Parameters.AddWithValue("@Codigo", codigo);
            cmd2.Parameters.AddWithValue("@Minutos", minutosValidez);

            await cmd2.ExecuteNonQueryAsync();
        }

        return codigo;
    }

    public async Task<bool> VerificarCodigoAsync(string correo, string codigo)
    {
        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(@"
                SELECT CodigoVerificacion, CodigoVerificacionExpira
                FROM Usuarios
                WHERE Correo = @Correo
                AND Activo = 1;
            ", conn))
        {
            cmd.Parameters.AddWithValue("@Correo", correo);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var codigoDb = reader["CodigoVerificacion"] as string;
                var expira = reader["CodigoVerificacionExpira"] as DateTime?;

                if (string.IsNullOrEmpty(codigoDb) || !expira.HasValue)
                    return false;

                if (DateTime.UtcNow > expira.Value)
                    return false;

                if (!string.Equals(codigoDb, codigo, StringComparison.Ordinal))
                    return false;

                reader.Close();

                using var cmdUpdate = new SqlCommand(@"
                    UPDATE Usuarios
                    SET EmailVerificado = 1,
                        CodigoVerificacion = NULL,
                        CodigoVerificacionExpira = NULL,
                        ActualizadoEl = SYSUTCDATETIME()
                    WHERE Correo = @Correo
                    AND Activo = 1;
                ", conn);

                cmdUpdate.Parameters.AddWithValue("@Correo", correo);
                await cmdUpdate.ExecuteNonQueryAsync();

                return true;
            }
        }
        using (var conn2 = new SqlConnection(_connectionString))
        using (var cmd2 = new SqlCommand(@"
            SELECT TOP 1 Codigo, Expira, FueVerificado, Usado
            FROM EmailVerifications
            WHERE Correo = @Correo
            ORDER BY Id DESC;
        ", conn2))
        {
            cmd2.Parameters.AddWithValue("@Correo", correo);

            await conn2.OpenAsync();
            using var reader2 = await cmd2.ExecuteReaderAsync();

            if (!await reader2.ReadAsync())
                return false;

            var codigoDb = reader2["Codigo"] as string;
            var expira = reader2["Expira"] as DateTime?;
            var fueVerificado = (bool)reader2["FueVerificado"];
            var usado = (bool)reader2["Usado"];

            if (usado)
                return false;

            if (fueVerificado)
                return true;

            if (string.IsNullOrEmpty(codigoDb) || !expira.HasValue)
                return false;

            if (DateTime.UtcNow > expira.Value)
                return false;

            if (!string.Equals(codigoDb, codigo, StringComparison.Ordinal))
                return false;
        }

        using (var conn3 = new SqlConnection(_connectionString))
        using (var cmd3 = new SqlCommand(@"
            UPDATE EmailVerifications
            SET FueVerificado = 1,
                VerificadoEl = SYSUTCDATETIME()
            WHERE Correo = @Correo
            AND Codigo = @Codigo
            AND Usado = 0;
        ", conn3))
        {
            cmd3.Parameters.AddWithValue("@Correo", correo);
            cmd3.Parameters.AddWithValue("@Codigo", codigo);

            await conn3.OpenAsync();
            var affected = await cmd3.ExecuteNonQueryAsync();
            return affected > 0;
        }
    }

    public async Task<string?> GenerarCodigoRecuperacionAsync(string correo, int minutosValidez = 15)
    {
        var codigo = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Usuarios
               SET CodigoRecuperacion = @Codigo,
                   CodigoRecuperacionExpira = DATEADD(MINUTE, @Minutos, SYSUTCDATETIME())
             WHERE Correo = @Correo
               AND Activo = 1;

            SELECT @@ROWCOUNT;
        ", conn);

        cmd.Parameters.AddWithValue("@Codigo", codigo);
        cmd.Parameters.AddWithValue("@Minutos", minutosValidez);
        cmd.Parameters.AddWithValue("@Correo", correo);

        await conn.OpenAsync();
        var rows = (int)await cmd.ExecuteScalarAsync();

        if (rows == 0)
        {
            return null;
        }

        return codigo;
    }
    public async Task<bool> ValidarCodigoRecuperacionAsync(string correo, string codigo)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT CodigoRecuperacion, CodigoRecuperacionExpira
            FROM Usuarios
            WHERE Correo = @Correo
              AND Activo = 1;
        ", conn);

        cmd.Parameters.AddWithValue("@Correo", correo);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return false;

        var codigoDb = reader["CodigoRecuperacion"] as string;
        var expira = reader["CodigoRecuperacionExpira"] as DateTime?;

        if (string.IsNullOrEmpty(codigoDb) || !expira.HasValue)
            return false;

        if (DateTime.UtcNow > expira.Value)
            return false;

        if (!string.Equals(codigoDb, codigo, StringComparison.Ordinal))
            return false;

        return true;
    }
    public async Task<bool> RestablecerContrasenaAsync(string correo, string codigo, string nuevoHash)
    {
        var esValido = await ValidarCodigoRecuperacionAsync(correo, codigo);
        if (!esValido)
            return false;

        var idUsuario = await ObtenerIdUsuarioPorCorreoAsync(correo);
        if (!idUsuario.HasValue)
            return false;

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_ActualizarHashContrasena", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
        cmd.Parameters.AddWithValue("@HashContrasena", nuevoHash);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        using var cmdClear = new SqlCommand(@"
            UPDATE Usuarios
               SET CodigoRecuperacion = NULL,
                   CodigoRecuperacionExpira = NULL,
                   ActualizadoEl = SYSUTCDATETIME()
             WHERE IdUsuario = @IdUsuario;
        ", conn);

        cmdClear.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
        await cmdClear.ExecuteNonQueryAsync();

        return true;
    }

    public async Task<bool> EstaCorreoPreverificadoAsync(string correo)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 FueVerificado, Expira, Usado
            FROM EmailVerifications
            WHERE Correo = @Correo
            ORDER BY Id DESC;
        ", conn);

        cmd.Parameters.AddWithValue("@Correo", correo);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return false;

        var fueVerificado = (bool)reader["FueVerificado"];
        var expira = reader["Expira"] as DateTime?;
        var usado = (bool)reader["Usado"];

        if (!fueVerificado || usado)
            return false;

        if (!expira.HasValue || DateTime.UtcNow > expira.Value)
            return false;

        return true;
    }

    public async Task MarcarVerificacionComoUsadaAsync(string correo)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE EmailVerifications
            SET Usado = 1,
                UsadoEl = SYSUTCDATETIME()
            WHERE Correo = @Correo
            AND FueVerificado = 1
            AND Usado = 0;
        ", conn);

        cmd.Parameters.AddWithValue("@Correo", correo);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }


    }
}
