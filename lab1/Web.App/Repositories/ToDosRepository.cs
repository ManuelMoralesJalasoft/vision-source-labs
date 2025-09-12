using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Web.App.Models;

namespace Web.App.Repositories;

internal sealed class ToDosRepository : IToDosRepository
{
    private readonly string _connectionString;

    public ToDosRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection => new SqlConnection(_connectionString);

    public async Task<ToDo> AddAsync(ToDo toDo, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection;

        var result = await connection.QuerySingleOrDefaultAsync<ToDo>(
            "sp_AddToDo",
            new
            {
                toDo.Id,
                toDo.Title,
                toDo.IsCompleted,
            },
            commandType: CommandType.StoredProcedure
        );

        if (result == null)
        {
            throw new Exception("Failed to insert the ToDo item.");
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection;

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_DeleteToDo",
            new { Id = id },
            commandType: CommandType.StoredProcedure
        );

        return result > 0;
    }

    public async Task<IEnumerable<ToDo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection;

        return await connection.QueryAsync<ToDo>(
            "sp_GetAllToDos",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<ToDo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection;

        return await connection.QuerySingleOrDefaultAsync<ToDo>(
            "sp_GetToDoById",
            new { Id = id },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> ToggleStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        using var connection = CreateConnection;

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_ToggleToDoStatus",
            new { Id = id },
            commandType: CommandType.StoredProcedure
        );

        return result > 0;
    }
}
