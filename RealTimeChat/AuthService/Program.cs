using AuthService.Configuration;
using AuthService.Data;
using AuthService.Endpoints;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// --- CONFIGURACIÓN JWT SETTINGS ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JwtSettings:SecretKey no está configurado. Verifique User Secrets o appsettings.json");
}

// --- CONFIGURACIÓN JWT AUTHENTICATION ---
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // En producción cambiar a true
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// --- CONFIGURACIÓN CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173",
            "http://localhost:4200",
            "http://localhost:8080"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// --- REGISTRAR SERVICIOS ---
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

// --- CONFIGURACIÓN SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- MIGRACIONES AUTOMÁTICAS ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var historyExists = await EfHistoryExists(context);

        if (!historyExists)
        {
            await context.Database.MigrateAsync();
        }

        // SEED DATA
        await SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o crear seed data");
    }
}

// --- CONFIGURACIÓN PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// --- MAPEAR ENDPOINTS ---
app.MapAuthEndpoints();

app.Run();

// MÉTODO DE SEED DATA
static async Task SeedData(AppDbContext context)
{
    // Evita fallos de seeding sobre el pooler IPv4; en ese caso sembrar manualmente.
    var cs = context.Database.GetConnectionString() ?? string.Empty;
    if (cs.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    // Solo crear usuarios de prueba si no existen
    if (!await context.Usuarios.AnyAsync())
    {
        var usuarios = new[]
        {
            new AuthService.Entities.Usuario
            {
                Nombre = "Admin User",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            },
            new AuthService.Entities.Usuario
            {
                Nombre = "Test User",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            }
        };

        context.Usuarios.AddRange(usuarios);
        await context.SaveChangesAsync();
    }
}

// Evita que EF intente recrear la tabla de historial de migraciones cuando ya existe (pooler IPv4).
static async Task<bool> EfHistoryExists(AppDbContext context)
{
    var conn = context.Database.GetDbConnection();
    var wasClosed = conn.State == ConnectionState.Closed;

    if (wasClosed)
    {
        await conn.OpenAsync();
    }

    try
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            select exists (
                select 1
                from pg_catalog.pg_class c
                join pg_namespace n on n.oid = c.relnamespace
                where n.nspname = 'public' and c.relname = '__EFMigrationsHistory'
            );
            """;

        var result = await cmd.ExecuteScalarAsync();
        return result is bool b && b;
    }
    finally
    {
        if (wasClosed && conn.State == ConnectionState.Open)
        {
            await conn.CloseAsync();
        }
    }
}
