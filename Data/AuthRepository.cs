using System.Data;
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

        // REGISTRO
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
                cmd.Parameters.AddWithValue("@VersionTerminos",
                    (object?)request.VersionTerminos ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IpAceptacion",
                    (object?)request.IpAceptacion ?? DBNull.Value);

                var idOutput = new SqlParameter("@IdUsuario", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(idOutput);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                idNuevoUsuario = (int)idOutput.Value;
            }

            // Obtenemos los datos completos llamando a sp_AutenticarUsuario
            var usuario = await ObtenerUsuarioPorCorreoYHashAsync(
                request.Correo,
                request.ContrasenaHash
            );

            return usuario;
        }

        // LOGIN
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            return await ObtenerUsuarioPorCorreoYHashAsync(request.Correo, request.ContrasenaHash);
        }

        // Método reutilizable
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
    }
}
