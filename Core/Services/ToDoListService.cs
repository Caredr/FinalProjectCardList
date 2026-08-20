using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;

namespace FinalProjectCardList.Core.Services
{
    internal class ToDoListService : IToDoListService
    {
        private const int maxNameLength = 10;

        // Репозиторий сохраняет списки в файлы — данные не теряются при перезапуске
        private readonly IToDoListRepository _listRepository;
        private readonly IToDoService _todoService;
        private readonly IUserRepository _userRepository;

        public ToDoListService(
            IToDoListRepository listRepository,
            IToDoService todoService,
            IUserRepository userRepository)  // ← внедряем
        {
            _listRepository = listRepository ?? throw new ArgumentNullException(nameof(listRepository));
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<ToDoList> AddAsync(ToDoUser user, string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя не должно отсутствовать", nameof(name));

            if (name.Length > maxNameLength)
                throw new ArgumentException($"Имя не должно быть больше {maxNameLength} букв.", nameof(name));
            // ← ПРОВЕРКА: существует ли пользователь в БД

            var existingUser = await _userRepository.GetUser(user.UserId, ct);
            if (existingUser == null)
                throw new InvalidOperationException($"Пользователь {user.UserId} не найден в БД");
            // Проверяем дубликат через репозиторий (файлы), а не Dictionary
            if (await _listRepository.ExistsByName(user.UserId, name, ct))
                throw new ArgumentException($"Список с именем '{name}' уже существует.", nameof(name));

            var list = new ToDoList
            {
                Id = Guid.NewGuid(),
                Name = name,
                UserId = user.UserId
            };

            // Сохраняем в файл — список доступен после перезапуска
            await _listRepository.Add(list, ct);

            return list;
        }

        public async Task<ToDoList?> GetAsync(Guid id, CancellationToken ct)
        {
            // Читаем из файла, а не из памяти
            return await _listRepository.Get(id, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            // Удаляем файл
            await _listRepository.Delete(id, ct);
        }

        public async Task<IReadOnlyList<ToDoList>> GetUserListsAsync(Guid userId, CancellationToken ct)
        {
            // Читаем все списки пользователя из файлов
            return await _listRepository.GetByUserId(userId, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken ct)
        {
            return await _todoService.GetByUserIdAndList(userId, listId, ct);
        }
    }
}
