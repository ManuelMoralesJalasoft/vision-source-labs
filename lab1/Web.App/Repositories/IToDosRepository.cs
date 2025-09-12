using Web.App.Models;

namespace Web.App.Repositories;

internal interface IToDosRepository
{
    Task<IEnumerable<ToDo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ToDo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ToDo> AddAsync(ToDo toDo, CancellationToken cancellationToken = default);
    Task<bool> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
