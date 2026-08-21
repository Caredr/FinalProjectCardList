using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Exeptions;

namespace FinalProjectCardList.Core.Services
{
    internal class ToDoService : IToDoService
    {
        private readonly IToDoRepository _iToDoRepository;
        public ToDoService(IToDoRepository toDoRepository)
        {
            _iToDoRepository = toDoRepository;
        }
        public readonly int TaskCountLimit = 100;
        public readonly int TaskLengthLimitMax = 100;
        public readonly int TaskLengthLimitMin = 3;

        public async Task<ToDoItem> AddAsync(ToDoUser user,
        string name,
        ToDoList? list,
        DateTime deadline,
        int quantity,
        CancellationToken ct)
        {
            var item = new ToDoItem
            {
                Id = Guid.NewGuid(),
                UserId = user,  // ← ToDoUser
                ListId = list,  // ← ToDoList?
                Name = name,
                CreatedAt = DateTime.UtcNow,
                State = ToDoItemState.Active,
                Deadline = deadline == DateTime.MaxValue ? null : deadline,
                Quantity = quantity
            };

            await _iToDoRepository.Add(item, ct);

            return item;
        }
        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _iToDoRepository.GetActiveByUserId(userId, ct);
        }
        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _iToDoRepository.GetAllByUserId(userId, ct); 
        }
        public async Task MarkCompletedAsync(Guid id, CancellationToken ct)
        {
            var item = await _iToDoRepository.Get(id, ct);
            if (item == null)
                throw new TaskDoesNotExistException("Задача с таким GUID не существует");

            item.State = ToDoItemState.Completed;
            item.StateChangedAt = DateTime.UtcNow;
            await _iToDoRepository.Update(item, ct);
        }
        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            await _iToDoRepository.Delete(id, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct)
        {
            return await _iToDoRepository.Find(user.UserId, item =>
       item.Name?.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase) == true, ct);

        }

        public static void CountAdd()
        {
            Console.WriteLine("Введите максимальное количество задач"); //1
            string tasksCountstext = Console.ReadLine() ?? "Ошибка";   //2
            TasksLimit(tasksCountstext);

        }
        public async Task<IReadOnlyList<ToDoList>> GetListsByUserId(Guid userId, CancellationToken ct)
        {
            // Читаем все задачи из файлов и собираем уникальные списки
            var allItems = await _iToDoRepository.GetAllByUserId(userId, ct);
            var lists = allItems
       .Where(i => i.ListId is not null)   // у задачи есть список
       .Select(i => i.ListId!)             // берём сам ToDoList
       .GroupBy(l => l.Id)               // группируем по Id списка
       .Select(g => g.First())           // оставляем по одному экземпляру
       .ToList();
            return lists.AsReadOnly();
        }

        public async Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct)
        {
            return await _iToDoRepository.Get(toDoItemId, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct, ToDoItemState? stateFilter = null)
        {
            var allItems = await _iToDoRepository.GetAllByUserId(userId, ct); // ← берём ВСЕ задачи
            var filtered = allItems.Where(item =>
            {
                bool listMatch = listId.HasValue
    ? item.ListId is not null && item.ListId.Id == listId.Value
    : item.ListId is null;
                bool stateMatch = stateFilter == null || item.State == stateFilter;
                return listMatch && stateMatch;
            }).ToList();
            return filtered.AsReadOnly();
        }
        private static void ParseAndValidateInt(string? str, int min, int max)
        {
            str = ValidateString(str);
            int taskTextLenght = Translator(str);
            Validate(taskTextLenght, min, max);
        }
        #region CustomThrows
        public static int TasksLimit(string limit)
        {
            int taskCount = int.TryParse(limit, out int result) ? result : 0;
            if (taskCount <= 0 || taskCount > 100)
            {
                throw new ArgumentException("число должно быть больше 0-я и  меньше 100");
            }
            return taskCount;
        }
        //private int TestTo(string stringToTest)
        //{
        //    if (!int.TryParse(stringToTest, out int taskTextLenght))
        //    {
        //        throw new ArgumentException("Нельзя превратить текст в число");
        //    }
        //    return taskTextLenght;
        //}
        private static string ValidateString(string stringToTest)
        {
            if (string.IsNullOrWhiteSpace(stringToTest))
            {
                throw new ArgumentException("Строка не может быть пустой");
            }
            return stringToTest;
        }
        private static void Validate(int lenghtToTest, int minLenght, int maxLenght)
        {
            if (lenghtToTest < minLenght || lenghtToTest > maxLenght)
            {
                throw new ArgumentException("слишком короткое название или слишком длинное");
            }
        }
        public static int Translator(string stringToTest)
        {
            int taskTextLenght = stringToTest.Length;
            return taskTextLenght;
        }
        #endregion
    }
}
