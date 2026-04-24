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

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("UserId não encontrado");

    // 🔹 GET - listar tarefas do usuário
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var userId = GetUserId();

        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks);
    }

    // 🔹 POST - criar tarefa
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetUserId();

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
    }

    // 🔹 DELETE - apenas ADMIN
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}