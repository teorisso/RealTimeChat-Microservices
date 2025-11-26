using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Shared.DTOs;
using Shared.Responses;
using System.Security.Claims;

namespace AuthService.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Authentication");

            // POST /api/auth/register
            group.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
            {
                var result = await authService.RegisterAsync(request);

                if (!result.Success)
                {
                    return Results.BadRequest(ApiResponse<AuthResponse>.FailureResponse(
                        result.Message,
                        new List<string> { result.Message }
                    ));
                }

                return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(
                    result,
                    "Usuario registrado exitosamente"
                ));
            })
            .WithName("Register")
            .WithOpenApi()
            .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status400BadRequest);

            // POST /api/auth/login
            group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
            {
                var result = await authService.LoginAsync(request);

                if (!result.Success)
                {
                    return Results.BadRequest(ApiResponse<AuthResponse>.FailureResponse(
                        result.Message,
                        new List<string> { result.Message }
                    ));
                }

                return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(
                    result,
                    "Login exitoso"
                ));
            })
            .WithName("Login")
            .WithOpenApi()
            .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status400BadRequest);

            // POST /api/auth/refresh
            group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RefreshTokenAsync(request.RefreshToken);

                if (!result.Success)
                {
                    return Results.BadRequest(ApiResponse<AuthResponse>.FailureResponse(
                        result.Message,
                        new List<string> { result.Message }
                    ));
                }

                return Results.Ok(ApiResponse<AuthResponse>.SuccessResponse(
                    result,
                    "Token renovado exitosamente"
                ));
            })
            .WithName("RefreshToken")
            .WithOpenApi()
            .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status400BadRequest);

            // POST /api/auth/logout [Authorize]
            group.MapPost("/logout", async (ClaimsPrincipal user, IAuthService authService) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await authService.RevokeTokenAsync(userId);

                if (!result)
                {
                    return Results.BadRequest(ApiResponse<object>.FailureResponse(
                        "Error al cerrar sesión"
                    ));
                }

                return Results.Ok(ApiResponse<object>.SuccessResponse(
                    null,
                    "Sesión cerrada exitosamente"
                ));
            })
            .RequireAuthorization()
            .WithName("Logout")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

            // GET /api/auth/profile [Authorize]
            group.MapGet("/profile", async (ClaimsPrincipal user, IAuthService authService) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var usuario = await authService.GetUserByIdAsync(userId);

                if (usuario == null)
                {
                    return Results.NotFound(ApiResponse<UsuarioDto>.FailureResponse(
                        "Usuario no encontrado"
                    ));
                }

                var usuarioDto = new UsuarioDto
                {
                    Id = usuario.Id.ToString(),
                    Nombre = usuario.Nombre,
                    Email = usuario.Email
                };

                return Results.Ok(ApiResponse<UsuarioDto>.SuccessResponse(
                    usuarioDto,
                    "Perfil obtenido exitosamente"
                ));
            })
            .RequireAuthorization()
            .WithName("GetProfile")
            .WithOpenApi()
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status404NotFound);

            // PUT /api/auth/profile [Authorize]
            group.MapPut("/profile", async (
                UpdateProfileRequest request,
                ClaimsPrincipal user,
                IAuthService authService) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await authService.UpdateProfileAsync(userId, request);

                if (!result)
                {
                    return Results.BadRequest(ApiResponse<object>.FailureResponse(
                        "Error al actualizar el perfil"
                    ));
                }

                return Results.Ok(ApiResponse<object>.SuccessResponse(
                    null,
                    "Perfil actualizado exitosamente"
                ));
            })
            .RequireAuthorization()
            .WithName("UpdateProfile")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // GET /api/auth/users [Authorize]
            group.MapGet("/users", async (IAuthService authService) =>
            {
                var usuarios = await authService.GetAllUsersAsync();

                var usuariosDto = usuarios.Select(u => new UsuarioDto
                {
                    Id = u.Id.ToString(),
                    Nombre = u.Nombre,
                    Email = u.Email
                }).ToList();

                return Results.Ok(ApiResponse<List<UsuarioDto>>.SuccessResponse(
                    usuariosDto,
                    "Usuarios obtenidos exitosamente"
                ));
            })
            .RequireAuthorization()
            .WithName("GetAllUsers")
            .WithOpenApi()
            .Produces<ApiResponse<List<UsuarioDto>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

            // GET /api/auth/users/{id} [Authorize]
            group.MapGet("/users/{id:int}", async (int id, IAuthService authService) =>
            {
                var usuario = await authService.GetUserByIdAsync(id);

                if (usuario == null)
                {
                    return Results.NotFound(ApiResponse<UsuarioDto>.FailureResponse(
                        "Usuario no encontrado"
                    ));
                }

                var usuarioDto = new UsuarioDto
                {
                    Id = usuario.Id.ToString(),
                    Nombre = usuario.Nombre,
                    Email = usuario.Email
                };

                return Results.Ok(ApiResponse<UsuarioDto>.SuccessResponse(
                    usuarioDto,
                    "Usuario obtenido exitosamente"
                ));
            })
            .RequireAuthorization()
            .WithName("GetUserById")
            .WithOpenApi()
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<UsuarioDto>>(StatusCodes.Status404NotFound);
        }
    }
}
