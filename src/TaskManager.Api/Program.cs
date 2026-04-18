using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Banco de dados (SQLite) ─────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Core Identity ───────────────────────────────────────────
// Registra o sistema de identidade com AppUser (IdentityUser customizado)
// e IdentityRole para controle de papéis (Admin, User).
// O Identity cuida de: hash de senha, validação, lockout e armazenamento.
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        // Regras de senha simplificadas para ambiente didático.
        // Em produção, usem os padrões ou regras mais rígidas.
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── JWT Bearer ──────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key não configurado. Verifique appsettings.Development.json.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
    {
        // IMPORTANTE: sem estas duas linhas, o Identity usa cookies como
        // esquema padrão e o [Authorize] nos controllers não funciona com JWT.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Remove tolerância padrão de 5 min
        };
    });

// ── Autorização: Policies ───────────────────────────────────────────
// A Policy "CanDeleteTask" exige que o usuário tenha a role "Admin".
// Policies são mais flexíveis que [Authorize(Roles = "...")]:
// permitem combinar roles, claims e lógica customizada em um único lugar.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanDeleteTask", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Seed: cria as roles padrão se não existirem ─────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ORDEM IMPORTA: Authentication ANTES de Authorization.
// Authentication decodifica e valida o token (quem é você?).
// Authorization verifica permissões (o que você pode fazer?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
