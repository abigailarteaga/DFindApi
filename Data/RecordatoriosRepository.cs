using System.Data;
using DFindApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DFindApi.Data
{
    public class RecordatoriosRepository
    {
        private readonly string _connectionString;
        private async Task InsertarRecordatorioAsync(
        SqlConnection conn,
        SqlTransaction tx,
        string idRecordatorio,
        DateTime fecha,
        RecordatorioRequest request)
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO Recordatorios (
                    IdRecordatorio, IdUsuario, Titulo, Descripcion, FechaHora, 
                    Prioridad, Ubicacion, Objeto, EsRepetitivo, FrecuenciaRepeticion, 
                    Activo, Color, RutaImagen, DiasSeleccionados, CreadoEl, ActualizadoEl
                ) VALUES (
                    @IdRecordatorio, @IdUsuario, @Titulo, @Descripcion, @FechaHora,
                    @Prioridad, @Ubicacion, @Objeto, @EsRepetitivo, @FrecuenciaRepeticion,
                    @Activo, @Color, @RutaImagen, @DiasSeleccionados, SYSUTCDATETIME(), SYSUTCDATETIME()
                );", conn, tx);

            cmd.Parameters.AddWithValue("@IdRecordatorio", idRecordatorio);
            cmd.Parameters.AddWithValue("@IdUsuario", request.IdUsuario);
            cmd.Parameters.AddWithValue("@Titulo", request.Titulo);
            cmd.Parameters.AddWithValue("@Descripcion", (object?)request.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaHora", fecha);
            cmd.Parameters.AddWithValue("@Prioridad", request.Prioridad);
            cmd.Parameters.AddWithValue("@Ubicacion", request.Ubicacion);
            cmd.Parameters.AddWithValue("@Objeto", request.Objeto);
            cmd.Parameters.AddWithValue("@EsRepetitivo", request.EsRepetitivo);
            cmd.Parameters.AddWithValue("@FrecuenciaRepeticion", request.FrecuenciaRepeticion);
            cmd.Parameters.AddWithValue("@Activo", request.Activo);
            cmd.Parameters.AddWithValue("@Color", request.Color);
            cmd.Parameters.AddWithValue("@RutaImagen", (object?)request.RutaImagen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DiasSeleccionados", request.DiasSeleccionados);

            await cmd.ExecuteNonQueryAsync();
        }


        public RecordatoriosRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new Exception("Connection string no encontrada.");
        }
        private IEnumerable<DateTime> CalcularFechasRepeticion(RecordatorioRequest request)
        {
            var fechas = new List<DateTime>();

            var fechaInicio = request.FechaHora;
            var fechaFin = fechaInicio.AddMonths(3);

            var frecuenciaRaw = request.FrecuenciaRepeticion ?? string.Empty;
            var frecuencia = frecuenciaRaw
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", ""); 

            var diasSeleccionados = ParseDiasSeleccionados(request.DiasSeleccionados);

            if (frecuencia == "diario" || frecuencia == "diaria")
            {
                var f = fechaInicio.AddDays(1);

                while (f <= fechaFin)
                {
                    fechas.Add(f);
                    f = f.AddDays(1);
                }
            }
            else if (frecuencia == "semanal")
            {
                var f = fechaInicio.AddDays(7);

                while (f <= fechaFin)
                {
                    fechas.Add(f);
                    f = f.AddDays(7);
                }
            }
            else if (frecuencia == "diassemana")
            {
                var f = fechaInicio.AddDays(1);

                while (f <= fechaFin)
                {
                    if (diasSeleccionados.Count == 0)
                    {
                        if (f.DayOfWeek == fechaInicio.DayOfWeek)
                        {
                            fechas.Add(f);
                        }
                    }
                    else
                    {
                        if (diasSeleccionados.Contains(f.DayOfWeek))
                        {
                            fechas.Add(f);
                        }
                    }

                    f = f.AddDays(1);
                }
            }

            return fechas;
        }
        private static HashSet<DayOfWeek> ParseDiasSeleccionados(string diasSeleccionados)
        {
            var resultado = new HashSet<DayOfWeek>();

            if (string.IsNullOrWhiteSpace(diasSeleccionados))
                return resultado;

            var partes = diasSeleccionados.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in partes)
            {
                if (int.TryParse(p, out int numeroDia) && numeroDia >= 0 && numeroDia <= 6)
                {
                    resultado.Add((DayOfWeek)numeroDia);
                }
            }

            return resultado;
        }
        public async Task<RecordatorioResponse?> CrearRecordatorioAsync(RecordatorioRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();

            try
            {
                var idRecordatorioPrincipal = request.IdRecordatorio ?? Guid.NewGuid().ToString();

                await InsertarRecordatorioAsync(
                    conn,
                    tx,
                    idRecordatorioPrincipal,
                    request.FechaHora,
                    request
                );

                if (request.EsRepetitivo && !string.IsNullOrWhiteSpace(request.FrecuenciaRepeticion))
                {
                    var fechasRepeticion = CalcularFechasRepeticion(request);

                    foreach (var fecha in fechasRepeticion)
                    {
                        var nuevoId = Guid.NewGuid().ToString();

                        await InsertarRecordatorioAsync(
                            conn,
                            tx,
                            nuevoId,
                            fecha,
                            request
                        );
                    }
                }

                tx.Commit();

                return await ObtenerRecordatorioPorIdAsync(idRecordatorioPrincipal);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }


        public async Task<List<RecordatorioResponse>> ObtenerRecordatoriosPorCorreoAsync(string correo)
        {
            var recordatorios = new List<RecordatorioResponse>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT r.IdRecordatorio, r.IdUsuario, r.Titulo, r.Descripcion, r.FechaHora,
                       r.Prioridad, r.Ubicacion, r.Objeto, r.EsRepetitivo, r.FrecuenciaRepeticion,
                       r.Activo, r.Color, r.RutaImagen, r.DiasSeleccionados, r.CreadoEl, r.ActualizadoEl
                FROM Recordatorios r
                INNER JOIN Usuarios u ON r.IdUsuario = u.IdUsuario
                WHERE u.Correo = @Correo
                ORDER BY r.FechaHora ASC;", conn))
            {
                cmd.Parameters.AddWithValue("@Correo", correo);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    recordatorios.Add(new RecordatorioResponse
                    {
                        IdRecordatorio = reader["IdRecordatorio"].ToString() ?? "",
                        IdUsuario = (int)reader["IdUsuario"],
                        Titulo = reader["Titulo"].ToString() ?? "",
                        Descripcion = reader["Descripcion"].ToString() ?? "",
                        FechaHora = (DateTime)reader["FechaHora"],
                        Prioridad = reader["Prioridad"].ToString() ?? "",
                        Ubicacion = reader["Ubicacion"].ToString() ?? "",
                        Objeto = reader["Objeto"].ToString() ?? "",
                        EsRepetitivo = (bool)reader["EsRepetitivo"],
                        FrecuenciaRepeticion = reader["FrecuenciaRepeticion"].ToString() ?? "",
                        Activo = (bool)reader["Activo"],
                        Color = reader["Color"].ToString() ?? "",
                        RutaImagen = reader["RutaImagen"] as string,
                        DiasSeleccionados = reader["DiasSeleccionados"].ToString() ?? "",
                        CreadoEl = (DateTime)reader["CreadoEl"],
                        ActualizadoEl = (DateTime)reader["ActualizadoEl"]
                    });
                }
            }

            return recordatorios;
        }

        public async Task<RecordatorioResponse?> ActualizarRecordatorioPorTituloAsync(string titulo, ActualizarRecordatorioPorTituloRequest request)
        {
            var parametrosActualizacion = new List<string>();
            var parametros = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(request.Descripcion))
            {
                parametrosActualizacion.Add("Descripcion = @Descripcion");
                parametros.Add(new SqlParameter("@Descripcion", request.Descripcion));
            }

            if (request.FechaHora.HasValue)
            {
                parametrosActualizacion.Add("FechaHora = @FechaHora");
                parametros.Add(new SqlParameter("@FechaHora", request.FechaHora.Value));
            }

            if (!string.IsNullOrEmpty(request.Prioridad))
            {
                parametrosActualizacion.Add("Prioridad = @Prioridad");
                parametros.Add(new SqlParameter("@Prioridad", request.Prioridad));
            }

            if (!string.IsNullOrEmpty(request.Ubicacion))
            {
                parametrosActualizacion.Add("Ubicacion = @Ubicacion");
                parametros.Add(new SqlParameter("@Ubicacion", request.Ubicacion));
            }

            if (!string.IsNullOrEmpty(request.Objeto))
            {
                parametrosActualizacion.Add("Objeto = @Objeto");
                parametros.Add(new SqlParameter("@Objeto", request.Objeto));
            }

            if (request.EsRepetitivo.HasValue)
            {
                parametrosActualizacion.Add("EsRepetitivo = @EsRepetitivo");
                parametros.Add(new SqlParameter("@EsRepetitivo", request.EsRepetitivo.Value));
            }

            if (!string.IsNullOrEmpty(request.FrecuenciaRepeticion))
            {
                parametrosActualizacion.Add("FrecuenciaRepeticion = @FrecuenciaRepeticion");
                parametros.Add(new SqlParameter("@FrecuenciaRepeticion", request.FrecuenciaRepeticion));
            }

            if (request.Activo.HasValue)
            {
                parametrosActualizacion.Add("Activo = @Activo");
                parametros.Add(new SqlParameter("@Activo", request.Activo.Value));
            }

            if (!string.IsNullOrEmpty(request.Color))
            {
                parametrosActualizacion.Add("Color = @Color");
                parametros.Add(new SqlParameter("@Color", request.Color));
            }

            if (request.RutaImagen != null)
            {
                parametrosActualizacion.Add("RutaImagen = @RutaImagen");
                parametros.Add(new SqlParameter("@RutaImagen", (object?)request.RutaImagen ?? DBNull.Value));
            }

            if (!string.IsNullOrEmpty(request.DiasSeleccionados))
            {
                parametrosActualizacion.Add("DiasSeleccionados = @DiasSeleccionados");
                parametros.Add(new SqlParameter("@DiasSeleccionados", request.DiasSeleccionados));
            }

            if (parametrosActualizacion.Count == 0)
            {
                return await ObtenerRecordatorioPorTituloAsync(titulo);
            }
            parametrosActualizacion.Add("ActualizadoEl = SYSUTCDATETIME()");

            var sql = $@"
                UPDATE Recordatorios 
                SET {string.Join(", ", parametrosActualizacion)}
                WHERE Titulo = @Titulo;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Titulo", titulo);
                foreach (var param in parametros)
                {
                    cmd.Parameters.Add(param);
                }

                await conn.OpenAsync();
                var filasAfectadas = await cmd.ExecuteNonQueryAsync();

                if (filasAfectadas == 0)
                    return null;
            }

            return await ObtenerRecordatorioPorTituloAsync(titulo);
        }

        public async Task<bool> EliminarRecordatorioPorTituloAsync(string titulo)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                DELETE FROM Recordatorios 
                WHERE Titulo = @Titulo;", conn))
            {
                cmd.Parameters.AddWithValue("@Titulo", titulo);

                await conn.OpenAsync();
                var filasAfectadas = await cmd.ExecuteNonQueryAsync();

                return filasAfectadas > 0;
            }
        }

        public async Task<RecordatorioResponse?> CambiarEstadoPorTituloAsync(string titulo)
        {
            var recordatorioActual = await ObtenerRecordatorioPorTituloAsync(titulo);
            
            if (recordatorioActual == null)
                return null;

            var nuevoEstado = !recordatorioActual.Activo;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                UPDATE Recordatorios 
                SET Activo = @NuevoEstado, ActualizadoEl = SYSUTCDATETIME()
                WHERE Titulo = @Titulo;", conn))
            {
                cmd.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);
                cmd.Parameters.AddWithValue("@Titulo", titulo);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return await ObtenerRecordatorioPorTituloAsync(titulo);
        }

        private async Task<RecordatorioResponse?> ObtenerRecordatorioPorTituloAsync(string titulo)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT IdRecordatorio, IdUsuario, Titulo, Descripcion, FechaHora,
                       Prioridad, Ubicacion, Objeto, EsRepetitivo, FrecuenciaRepeticion,
                       Activo, Color, RutaImagen, DiasSeleccionados, CreadoEl, ActualizadoEl
                FROM Recordatorios
                WHERE Titulo = @Titulo;", conn))
            {
                cmd.Parameters.AddWithValue("@Titulo", titulo);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                return new RecordatorioResponse
                {
                    IdRecordatorio = reader["IdRecordatorio"].ToString() ?? "",
                    IdUsuario = (int)reader["IdUsuario"],
                    Titulo = reader["Titulo"].ToString() ?? "",
                    Descripcion = reader["Descripcion"].ToString() ?? "",
                    FechaHora = (DateTime)reader["FechaHora"],
                    Prioridad = reader["Prioridad"].ToString() ?? "",
                    Ubicacion = reader["Ubicacion"].ToString() ?? "",
                    Objeto = reader["Objeto"].ToString() ?? "",
                    EsRepetitivo = (bool)reader["EsRepetitivo"],
                    FrecuenciaRepeticion = reader["FrecuenciaRepeticion"].ToString() ?? "",
                    Activo = (bool)reader["Activo"],
                    Color = reader["Color"].ToString() ?? "",
                    RutaImagen = reader["RutaImagen"] as string,
                    DiasSeleccionados = reader["DiasSeleccionados"].ToString() ?? "",
                    CreadoEl = (DateTime)reader["CreadoEl"],
                    ActualizadoEl = (DateTime)reader["ActualizadoEl"]
                };
            }
        }

        private async Task<RecordatorioResponse?> ObtenerRecordatorioPorIdAsync(string idRecordatorio)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT IdRecordatorio, IdUsuario, Titulo, Descripcion, FechaHora,
                       Prioridad, Ubicacion, Objeto, EsRepetitivo, FrecuenciaRepeticion,
                       Activo, Color, RutaImagen, DiasSeleccionados, CreadoEl, ActualizadoEl
                FROM Recordatorios
                WHERE IdRecordatorio = @IdRecordatorio;", conn))
            {
                cmd.Parameters.AddWithValue("@IdRecordatorio", idRecordatorio);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                return new RecordatorioResponse
                {
                    IdRecordatorio = reader["IdRecordatorio"].ToString() ?? "",
                    IdUsuario = (int)reader["IdUsuario"],
                    Titulo = reader["Titulo"].ToString() ?? "",
                    Descripcion = reader["Descripcion"].ToString() ?? "",
                    FechaHora = (DateTime)reader["FechaHora"],
                    Prioridad = reader["Prioridad"].ToString() ?? "",
                    Ubicacion = reader["Ubicacion"].ToString() ?? "",
                    Objeto = reader["Objeto"].ToString() ?? "",
                    EsRepetitivo = (bool)reader["EsRepetitivo"],
                    FrecuenciaRepeticion = reader["FrecuenciaRepeticion"].ToString() ?? "",
                    Activo = (bool)reader["Activo"],
                    Color = reader["Color"].ToString() ?? "",
                    RutaImagen = reader["RutaImagen"] as string,
                    DiasSeleccionados = reader["DiasSeleccionados"].ToString() ?? "",
                    CreadoEl = (DateTime)reader["CreadoEl"],
                    ActualizadoEl = (DateTime)reader["ActualizadoEl"]
                };
            }
        }
        public async Task<bool> EliminarMoviendoAPapeleraPorTituloAsync(
            string correoUsuario,
            string titulo)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_MoverRecordatorioAPapeleraPorTitulo", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CorreoUsuario", correoUsuario);
            cmd.Parameters.AddWithValue("@Titulo", titulo);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException ex) when (ex.Message.Contains("RECORDATORIO_NO_ENCONTRADO"))
            {
                return false;
            }
        }

        public async Task<bool> RestaurarRecordatorioDesdePapeleraPorTituloAsync(
            string correoUsuario,
            string titulo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RestaurarRecordatorioDesdePapeleraPorTitulo", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CorreoUsuario", correoUsuario);
            cmd.Parameters.AddWithValue("@Titulo", titulo);

            await conn.OpenAsync();

            try
            {
                var filasAfectadas = await cmd.ExecuteNonQueryAsync();
                return filasAfectadas > 0;
            }
            catch (SqlException ex)
            {
                throw;
            }
        }
    }
}