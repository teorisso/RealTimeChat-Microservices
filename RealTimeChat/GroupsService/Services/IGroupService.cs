using GroupsService.DTOs;
using Shared.DTOs;

namespace GroupsService.Services
{
    public interface IGroupService
    {
        Task<GrupoDto?> CreateGroupAsync(int creadorId, CreateGrupoRequest request);
        Task<GrupoDto?> GetGroupAsync(int grupoId, int userId);
        Task<List<GrupoDto>> GetUserGroupsAsync(int userId);
        Task<bool> UpdateGroupAsync(int grupoId, int userId, UpdateGrupoRequest request);
        Task<bool> DeleteGroupAsync(int grupoId, int userId);
        Task<bool> AddMemberAsync(int grupoId, int userId, AddMemberRequest request);
        Task<bool> RemoveMemberAsync(int grupoId, int userId, int miembroId);
        Task<bool> IsUserMemberAsync(int grupoId, int userId);
        Task<bool> IsUserAdminAsync(int grupoId, int userId);
    }
}
