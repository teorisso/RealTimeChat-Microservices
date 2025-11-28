using MessagesService.Configuration;
using MessagesService.Data;
using MessagesService.Endpoints;
using MessagesService.Hubs;
using MessagesService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN JSON (camelCase para compatibilidad con frontend) ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// --- CONFIGURACIÓN BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MessagesDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Deshabilitar batching para compatibilidad con Supabase Transaction Pooler
        npgsqlOptions.MaxBatchSize(1);
    });
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

    // Configuración para SignalR - permite JWT en query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // Si la request es para el hub de SignalR y tiene token en query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
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
builder.Services.AddScoped<IMessageService, MessageServiceImpl>();

// Servicio de consulta de nombres de usuarios con JWT propagation (REQ-10)
builder.Services.AddHttpContextAccessor();  // CRÍTICO para propagar JWT
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IUserInfoService, UserInfoService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5257");
});

// --- CONFIGURACIÓN SIGNALR ---
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

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
        var context = services.GetRequiredService<MessagesDbContext>();
        var messagesMigrationExists = await MessagesMigrationExists(context);

        if (!messagesMigrationExists)
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MessagesService API v1");
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
app.MapMessageEndpoints();

// --- MAPEAR SIGNALR HUB ---
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

// MÉTODO DE SEED DATA
static async Task SeedData(MessagesDbContext context)
{
    // Evita fallos de seeding sobre el pooler IPv4
    var cs = context.Database.GetConnectionString() ?? string.Empty;
    if (cs.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    // Seed data opcional aquí
    // Por ahora no agregamos datos de prueba
}

// Verifica si las migraciones específicas de MessagesService ya están aplicadas
static async Task<bool> MessagesMigrationExists(MessagesDbContext context)
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
        // Verificar si existe alguna migración específica de MessagesService
        // Nota: Ajustar el nombre cuando se cree la migración
        cmd.CommandText = """
            select exists (
                select 1
                from "__EFMigrationsHistory"
                where "MigrationId" LIKE '%MessagesSchema%'
            );
            """;

        var result = await cmd.ExecuteScalarAsync();
        return result is bool b && b;
    }
    catch
    {
        // Si la tabla __EFMigrationsHistory no existe o hay error, retornar false
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
