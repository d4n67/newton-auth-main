namespace TaskManager.Api.Models;

/// <summary>
/// Representa uma tarefa no sistema.
/// Cada tarefa pertence a um usuário (UserId).
/// </summary>
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relacionamento: cada tarefa pertence a um usuário
    public string UserId { get; set; } = string.Empty;
}
