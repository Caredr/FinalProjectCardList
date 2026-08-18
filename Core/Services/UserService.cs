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
            ArgumentNullException.ThrowIfNull(telegramUserName, nameof(telegramUserName));
            var userСurrent = new ToDoUser
            {
                TelegramUserId = telegramUserId,
                TelegramUserName = telegramUserName
            };
             _iUserRepository.Add(userСurrent, ct);
            return await Task.FromResult(userСurrent);
        }

    }
}
