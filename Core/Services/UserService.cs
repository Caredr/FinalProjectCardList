using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Services
{
    internal class UserService : IUserService
    {
        private IUserRepository _iUserRepository;
        public UserService(IUserRepository iUserRepository)
        {
            _iUserRepository = iUserRepository;
        }

        public async Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken ct)
        {
            return await _iUserRepository.GetUserByTelegramUserId(telegramUserId, ct);
        }
        public async Task<ToDoUser> RegisterUser(long telegramUserId, string telegramUserName, CancellationToken ct)
        {
            var existingUser =
    await _iUserRepository.GetUserByTelegramUserId(telegramUserId, ct);

            if (existingUser != null)
                return existingUser;

            var user = new ToDoUser
            {
                UserId = Guid.NewGuid(),
                TelegramUserId = telegramUserId,
                TelegramUserName = telegramUserName,
                RegisteredAt = DateTime.UtcNow
            };

            await _iUserRepository.Add(user, ct);

            return user;
        }

    }
}
