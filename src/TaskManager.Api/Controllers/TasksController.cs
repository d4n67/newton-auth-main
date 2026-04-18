using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TasksController> _logger;

    public TasksController(AppDbContext context, ILogger<TasksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Extrai o UserId do token JWT (claim "sub" / NameIdentifier).
    /// </summary>
    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("UserId não encontrado no token.");

    // ╔══════════════════════════════════════════════════════════════╗
    // ║  ETAPA 2 — Protejam os endpoints com [Authorize]           ║
    // ╚══════════════════════════════════════════════════════════════╝

    /// <summary>
    /// GET /api/tasks
    /// Retorna as tarefas do usuário autenticado.
    /// </summary>
    // TODO 2.1: Adicionem o atributo [Authorize] neste método.
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        // TODO 2.2: Extraiam o userId do token com GetUserId()
        //   e filtrem as tarefas para retornar APENAS as do usuário logado.
        //
        //   var userId = GetUserId();
        //   var tasks = await _context.Tasks
        //       .Where(t => t.UserId == userId)
        //       .OrderByDescending(t => t.CreatedAt)
        //       .ToListAsync();
        //   return Ok(tasks);

        throw new NotImplementedException("Implementem o GetTasks na Etapa 2");
    }

    /// <summary>
    /// POST /api/tasks
    /// Cria uma nova tarefa vinculada ao usuário autenticado.
    /// </summary>
    // TODO 2.3: Adicionem o atributo [Authorize] neste método.
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        // TODO 2.4: Extraiam o userId do token e criem a tarefa:
        //
        //   var userId = GetUserId();
        //   var task = new TaskItem
        //   {
        //       Title = request.Title,
        //       Description = request.Description,
        //       UserId = userId
        //   };
        //   _context.Tasks.Add(task);
        //   await _context.SaveChangesAsync();
        //
        //   _logger.LogInformation("Tarefa {TaskId} criada por {UserId}", task.Id, userId);
        //   return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);

        throw new NotImplementedException("Implementem o CreateTask na Etapa 2");
    }

    // ╔══════════════════════════════════════════════════════════════╗
    // ║  ETAPA 3 — Restrinjam o DELETE com a Policy "CanDeleteTask" ║
    // ╚══════════════════════════════════════════════════════════════╝

    /// <summary>
    /// DELETE /api/tasks/{id}
    /// Remove uma tarefa. Apenas Admin pode executar (Policy: CanDeleteTask).
    /// </summary>
    // TODO 3.2: Substituam [Authorize] por [Authorize(Policy = "CanDeleteTask")]
    //   para que apenas Admin consiga deletar tarefas.
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task is null)
            return NotFound(new { message = $"Tarefa {id} não encontrada." });

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tarefa {TaskId} deletada por {UserId}", id, GetUserId());
        return NoContent();
    }
}
