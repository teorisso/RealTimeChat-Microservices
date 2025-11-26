using GroupsService.DTOs;
using GroupsService.Services;
using Shared.DTOs;
using Shared.Responses;
using System.Security.Claims;

namespace GroupsService.Endpoints
{
    public static class GroupEndpoints
    {
        public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/groups").WithTags("Groups");

            // POST /api/groups - Crear grupo
            group.MapPost("/", async (
                CreateGrupoRequest request,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var grupo = await groupService.CreateGroupAsync(userId, request);

                if (grupo == null)
                {
                    return Results.BadRequest(ApiResponse<GrupoDto>.FailureResponse(
                        "No se pudo crear el grupo"));
                }

                return Results.Ok(ApiResponse<GrupoDto>.SuccessResponse(
                    grupo, "Grupo creado exitosamente"));
            })
            .RequireAuthorization()
            .WithName("CreateGroup")
            .WithOpenApi()
            .Produces<ApiResponse<GrupoDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<GrupoDto>>(StatusCodes.Status400BadRequest);

            // GET /api/groups - Obtener grupos del usuario
            group.MapGet("/", async (
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var grupos = await groupService.GetUserGroupsAsync(userId);

                return Results.Ok(ApiResponse<List<GrupoDto>>.SuccessResponse(
                    grupos, "Grupos obtenidos exitosamente"));
            })
            .RequireAuthorization()
            .WithName("GetUserGroups")
            .WithOpenApi()
            .Produces<ApiResponse<List<GrupoDto>>>(StatusCodes.Status200OK);

            // GET /api/groups/{id} - Obtener grupo específico
            group.MapGet("/{id:int}", async (
                int id,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var grupo = await groupService.GetGroupAsync(id, userId);

                if (grupo == null)
                {
                    return Results.NotFound(ApiResponse<GrupoDto>.FailureResponse(
                        "Grupo no encontrado o no tienes acceso"));
                }

                return Results.Ok(ApiResponse<GrupoDto>.SuccessResponse(
                    grupo, "Grupo obtenido exitosamente"));
            })
            .RequireAuthorization()
            .WithName("GetGroup")
            .WithOpenApi()
            .Produces<ApiResponse<GrupoDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<GrupoDto>>(StatusCodes.Status404NotFound);

            // PUT /api/groups/{id} - Actualizar grupo (solo admins)
            group.MapPut("/{id:int}", async (
                int id,
                UpdateGrupoRequest request,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var result = await groupService.UpdateGroupAsync(id, userId, request);

                if (!result)
                {
                    return Results.BadRequest(ApiResponse<object>.FailureResponse(
                        "No se pudo actualizar el grupo o no tienes permisos"));
                }

                return Results.Ok(ApiResponse<object>.SuccessResponse(
                    null, "Grupo actualizado exitosamente"));
            })
            .RequireAuthorization()
            .WithName("UpdateGroup")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // DELETE /api/groups/{id} - Eliminar grupo (solo creador)
            group.MapDelete("/{id:int}", async (
                int id,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var result = await groupService.DeleteGroupAsync(id, userId);

                if (!result)
                {
                    return Results.BadRequest(ApiResponse<object>.FailureResponse(
                        "No se pudo eliminar el grupo o no eres el creador"));
                }

                return Results.Ok(ApiResponse<object>.SuccessResponse(
                    null, "Grupo eliminado exitosamente"));
            })
            .RequireAuthorization()
            .WithName("DeleteGroup")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // POST /api/groups/{id}/members - Agregar miembro (solo admins)
            group.MapPost("/{id:int}/members", async (
                int id,
                AddMemberRequest request,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var result = await groupService.AddMemberAsync(id, userId, request);

                if (!result)
                {
                    return Results.BadRequest(ApiResponse<object>.FailureResponse(
                        "No se pudo agregar el miembro o no tienes permisos"));
                }

                return Results.Ok(ApiResponse<object>.SuccessResponse(
                    null, "Miembro agregado exitosamente"));
            })
            .RequireAuthorization()
            .WithName("AddMember")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // DELETE /api/groups/{id}/members/{memberId} - Remover miembro (solo admins)
            group.MapDelete("/{id:int}/members/{memberId:int}", async (
                int id,
                int memberId,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var result = await groupService.RemoveMemberAsync(id, userId, memberId);

                if (!result)
                {
                    return Results.BadRequest(ApiResponse<object>.FailureResponse(
                        "No se pudo remover el miembro o no tienes permisos"));
                }

                return Results.Ok(ApiResponse<object>.SuccessResponse(
                    null, "Miembro removido exitosamente"));
            })
            .RequireAuthorization()
            .WithName("RemoveMember")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

            // GET /api/groups/{id}/members - Obtener miembros del grupo
            group.MapGet("/{id:int}/members", async (
                int id,
                ClaimsPrincipal user,
                IGroupService groupService) =>
            {
                var userId = GetUserIdFromClaims(user);
                var grupo = await groupService.GetGroupAsync(id, userId);

                if (grupo == null)
                {
                    return Results.NotFound(ApiResponse<List<UsuarioDto>>.FailureResponse(
                        "Grupo no encontrado o no tienes acceso"));
                }

                return Results.Ok(ApiResponse<List<UsuarioDto>>.SuccessResponse(
                    grupo.Miembros, "Miembros obtenidos exitosamente"));
            })
            .RequireAuthorization()
            .WithName("GetGroupMembers")
            .WithOpenApi()
            .Produces<ApiResponse<List<UsuarioDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<List<UsuarioDto>>>(StatusCodes.Status404NotFound);
        }

        private static int GetUserIdFromClaims(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }
    }
}
