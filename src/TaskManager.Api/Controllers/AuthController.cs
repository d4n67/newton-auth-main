using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AppUser> userManager,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    // ╔══════════════════════════════════════════════════════════════╗
    // ║  ETAPA 1 — Implementação dos endpoints Register e Login       ║
    // ╚══════════════════════════════════════════════════════════════╝

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 1.1: Criar instância de AppUser com os dados do request
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        // 1.2: Criar o utilizador no Identity
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // 1.3: Atribuir a role "User" por padrão
        await _userManager.AddToRoleAsync(user, "User");

        // 1.4: Logar e retornar sucesso
        _logger.LogInformation("Utilizador {Email} registado com sucesso.", user.Email);
        return Created("", new { user.Id, user.Email, user.FullName, role = "User" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1.5: Buscar o utilizador pelo email
        var user = await _userManager.FindByEmailAsync(request.Email);

        // 1.6: Validar credenciais
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Falha no login para: {Email}", request.Email);
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        // 1.7: Buscar as roles para incluir no Token
        var roles = await _userManager.GetRolesAsync(user);

        // 1.8: Gerar o token JWT
        var token = GerarToken(user, roles);
        
        return Ok(new { token });
    }

    // ╔══════════════════════════════════════════════════════════════╗
    // ║  ETAPA 3 — Adição do endpoint RegisterAdmin                   ║
    // ╚══════════════════════════════════════════════════════════════╝

    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest request)
    {
        // 3.1: Lógica idêntica ao Register, mas com a role "Admin"
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, "Admin");

        _logger.LogInformation("Administrador {Email} registado com sucesso.", user.Email);
        return Created("", new { user.Id, user.Email, user.FullName, role = "Admin" });
    }

    // ═══════════════════════════════════════════════════════════════
    //  Método auxiliar para gerar JWT — JÁ IMPLEMENTADO
    // ═══════════════════════════════════════════════════════════════

    private string GerarToken(AppUser user, IList<string> roles)
    {
        var key = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expiresIn = int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim("fullName", user.FullName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresIn),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}