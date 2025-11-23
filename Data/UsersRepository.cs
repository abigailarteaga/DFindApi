// Data/UsersRepository.cs
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DFindApi.Models;

namespace DFindApi.Data
{
    public class UsersRepository
    {
        private readonly IConfiguration _config;

        public UsersRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection()
            => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task UpdateProfileAsync(UpdateProfileRequest request)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_ActualizarPerfilPorCorreo", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Correo", request.Correo);

            cmd.Parameters.AddWithValue("@NuevoCorreo",
                (object?)request.NuevoCorreo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NombreUsuario",
                (object?)request.NombreUsuario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TamanoFuente",
                (object?)request.TamanoFuente ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModoOscuro",
                (object?)request.ModoOscuro ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NotificacionesSonido",
                (object?)request.NotificacionesSonido ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NotificacionesVibracion",
                (object?)request.NotificacionesVibracion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AvatarTipo",
                (object?)request.AvatarTipo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AvatarClave",
                (object?)request.AvatarClave ?? DBNull.Value);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("El correo ya está registrado"))
                    throw new InvalidOperationException("EMAIL_DUPLICADO");
                if (ex.Message.Contains("Usuario no encontrado"))
                    throw new InvalidOperationException("USUARIO_NO_ENCONTRADO");

                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string correo)
        {
            const string sql = @"
            SELECT
                u.IdUsuario      AS id_usuario,
                u.NombreUsuario  AS nombre_usuario,
                u.Correo         AS email,
                u.ContrasenaHash AS contrasena_hash,
                u.RegistradoEl   AS fecha_creacion,
                ''               AS telefono,      -- si no tienes columna Teléfono
                u.AvatarTipo     AS avatar_tipo,
                u.AvatarClave    AS avatar_clave
            FROM dbo.Usuarios u
            WHERE u.Correo = @Correo;";


            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Correo", correo);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return MapToUser(reader);
        }
        private User MapToUser(SqlDataReader reader)
        {
            return new User
            {
                IdUsuario      = reader.GetInt32(reader.GetOrdinal("id_usuario")),
                NombreUsuario  = reader.GetString(reader.GetOrdinal("nombre_usuario")),
                Email          = reader.GetString(reader.GetOrdinal("email")),
                ContrasenaHash = reader.GetString(reader.GetOrdinal("contrasena_hash")),
                FechaCreacion  = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                Telefono       = reader.IsDBNull(reader.GetOrdinal("telefono"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("telefono")),
                AvatarTipo     = reader.IsDBNull(reader.GetOrdinal("avatar_tipo"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("avatar_tipo")),
                AvatarClave    = reader.IsDBNull(reader.GetOrdinal("avatar_clave"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("avatar_clave")),
            };
        }
    }
}
