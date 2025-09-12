namespace Web.App.Models;

internal sealed class ToDo
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Title { get; init; }

    public bool IsCompleted { get; set; } = false;
}
