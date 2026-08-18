using FinalProjectCardList.Core.Entities;


namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IUserRepository // Репозиторий для работы с данными пользователей. Он предоставляет методы для получения информации о пользователях и добавления новых пользователей в систему.
    {
        // Получает пользователя по его идентификатору. Если пользователь не найден, возвращает null.
        Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct);
        // Получает пользователя по его TelegramUserId. Если пользователь не найден, возвращает null.
        Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct);
        // Добавляет нового пользователя в систему. Принимает объект ToDoUser, который содержит информацию о пользователе, и сохраняет его в базе данных.
        Task Add(ToDoUser user, CancellationToken ct);
        // Возвращает всех пользователей
        Task<IReadOnlyList<ToDoUser?>> GetUsers(CancellationToken ct);
    }
}
