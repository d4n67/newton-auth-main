using Microsoft.AspNetCore.Identity;

namespace TaskManager.Api.Models;

/// <summary>
/// Usuário da aplicação. Herda de IdentityUser, que já inclui:
/// Id, UserName, Email, PasswordHash, PhoneNumber, etc.
/// Adicionem propriedades customizadas conforme necessário.
/// </summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
