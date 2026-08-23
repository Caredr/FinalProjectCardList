using FinalProjectCardList.Core.Entities;


namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IUserRepository 
    {
        Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct);
        Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct);
        Task Add(ToDoUser user, CancellationToken ct);
        Task<IReadOnlyList<ToDoUser?>> GetUsers(CancellationToken ct);
    }
}
