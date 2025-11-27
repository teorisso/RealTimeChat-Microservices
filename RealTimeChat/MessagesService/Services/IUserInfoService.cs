namespace MessagesService.Services
{
    public interface IUserInfoService
    {
        Task<string> GetUserNameAsync(int userId);
        Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds);
    }
}
