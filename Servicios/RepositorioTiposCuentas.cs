using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

public interface IRepositorioTiposCuentas
{
    Task Crear(TipoCuenta tipoCuenta);
    Task<bool> Existe(string Existe, int UsuarioId, int id = 0);
    Task<IEnumerable<TipoCuenta>> Obtener(int UsuarioId);
    Task Actualizar(TipoCuenta tipoCuenta);
    Task<TipoCuenta> ObtenerPorId(int id, int UsuarioId);
    Task Borrar(int id);
    Task Ordenar(IEnumerable<TipoCuenta> tiposCuentasOrdenados);
}

public class RepositorioTiposCuentas: IRepositorioTiposCuentas
{
    private readonly string connectionString;
    public RepositorioTiposCuentas(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task Crear(TipoCuenta tipoCuenta)
    {
        using var connection = new SqlConnection(connectionString);
        // NUEVO: Agregamos EsPasivo a los parámetros que se envían al Procedimiento Almacenado
        var id = await connection.QuerySingleAsync<int>("TiposCuentas_Insertar", new {
                                                            UsuarioId = tipoCuenta.UsuarioId,
                                                            Nombre = tipoCuenta.Nombre,
                                                            EsPasivo = tipoCuenta.EsPasivo 
                                                        }, commandType:System.Data.CommandType.StoredProcedure);

        tipoCuenta.Id = id;
    }

    public async Task<bool> Existe(string Nombre, int UsuarioId, int id = 0)
    {
        using var connection = new SqlConnection(connectionString);
        var existe= await connection.QueryFirstOrDefaultAsync<int>(@$"SELECT 1 FROM TiposCuentas 
                                                                       WHERE Nombre = @Nombre And UsuarioId = @UsuarioId AND Id <> @id;",
                                                                       new {Nombre, UsuarioId, id});
        return existe == 1;
    }

    public async Task<IEnumerable<TipoCuenta>> Obtener(int UsuarioId)
    {
        using var connection = new SqlConnection(connectionString);
        // NUEVO: Agregamos EsPasivo al SELECT
        return await connection.QueryAsync<TipoCuenta>(@"SELECT Id, Nombre, Orden, EsPasivo FROM TiposCuentas 
                                                           WHERE UsuarioId=@UsuarioId
                                                           ORDER BY Orden", new {UsuarioId});
    } 
    
    public async Task Actualizar(TipoCuenta tipoCuenta)
    {
        using var connection = new SqlConnection(connectionString);
        // NUEVO: Agregamos EsPasivo al UPDATE
        await connection.ExecuteAsync(@"UPDATE TiposCuentas SET Nombre=@Nombre, EsPasivo=@EsPasivo WHERE Id=@Id", tipoCuenta);
    }

    public async Task<TipoCuenta> ObtenerPorId(int id, int UsuarioId)
    {
        using var connection = new SqlConnection(connectionString);
        // NUEVO: Agregamos EsPasivo al SELECT
        return await connection.QueryFirstOrDefaultAsync<TipoCuenta>(@"SELECT Id, Nombre, Orden, EsPasivo FROM TiposCuentas
                                                                         WHERE Id = @Id AND UsuarioId= @UsuarioId",
                                                                         new {id, UsuarioId});
    }

    public async Task Borrar(int id)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync(@"DELETE TiposCuentas WHERE Id = @Id", new { id });
    }

    public async Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasOrdenados)
    {
        var query = "UPDATE TiposCuentas SET Orden = @Orden WHERE Id = @Id";
        using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync(query, tipoCuentasOrdenados);
    }
}