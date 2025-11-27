using GroupsService.Configuration;
using GroupsService.Data;
using GroupsService.Endpoints;
using GroupsService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<GroupsDbContext>(options =>
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
builder.Services.AddScoped<IGroupService, GroupServiceImpl>();

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
        var context = services.GetRequiredService<GroupsDbContext>();
        var groupsMigrationExists = await GroupsMigrationExists(context);

        if (!groupsMigrationExists)
        {
            await context.Database.MigrateAsync();
        }

        // SEED DATA (opcional)
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GroupsService API v1");
    });
}

// Solo redireccionar a HTTPS en producción
// En desarrollo, el frontend usa HTTP (localhost:5173) y el backend HTTP (localhost:5257)
// La redirección HTTPS rompe las peticiones CORS preflight
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// --- MAPEAR ENDPOINTS ---
app.MapGroupEndpoints();

app.Run();

// MÉTODO DE SEED DATA
static async Task SeedData(GroupsDbContext context)
{
    // Evita fallos de seeding sobre el pooler IPv4; en ese caso sembrar manualmente.
    var cs = context.Database.GetConnectionString() ?? string.Empty;
    if (cs.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    // Seed data opcional aquí
    // Por ahora no agregamos datos de prueba
}

// Verifica si las migraciones específicas de GroupsService ya están aplicadas
static async Task<bool> GroupsMigrationExists(GroupsDbContext context)
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
        // Verificar si existe la migración específica de GroupsService
        cmd.CommandText = """
            select exists (
                select 1
                from "__EFMigrationsHistory"
                where "MigrationId" = '20251126161627_InitialGroupsSchema'
            );
            """;

        var result = await cmd.ExecuteScalarAsync();
        return result is bool b && b;
    }
    catch
    {
        // Si la tabla __EFMigrationsHistory no existe, retornar false
        return false;
    }
    finally
    {
        if (wasClosed && conn.State == ConnectionState.Open)
        {
            await conn.CloseAsync();
        }
    }
}
