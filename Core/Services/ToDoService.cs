using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Exeptions;
using System;

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
                UserId = user,  
                ListId = list,  
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
            Console.WriteLine("Введите максимальное количество задач"); 
            string tasksCountstext = Console.ReadLine() ?? "Ошибка";   
            TasksLimit(tasksCountstext);

        }
        public async Task<IReadOnlyList<ToDoList>> GetListsByUserId(Guid userId, CancellationToken ct)
        {
            var allItems = await _iToDoRepository.GetAllByUserId(userId, ct);
            var lists = allItems
       .Where(i => i.ListId is not null)   
       .Select(i => i.ListId!)             
       .GroupBy(l => l.Id)              
       .Select(g => g.First())          
       .ToList();
            return lists.AsReadOnly();
        }

        public async Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct)
        {
            return await _iToDoRepository.Get(toDoItemId, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct, ToDoItemState? stateFilter = null)
        {
            var allItems = await _iToDoRepository.GetAllByUserId(userId, ct); 
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
        public async Task MoveTaskToListAsync(
    Guid taskId,
    ToDoList? targetList,
    CancellationToken ct)
        {
            var task = await Get(taskId, ct);

            if (task == null)
                throw new InvalidOperationException("Задача не найдена.");

            task.ListId = targetList;

            await _iToDoRepository.Update(task, ct);
        }
        public async Task<IReadOnlyList<ToDoItem>> GetAllAsync(CancellationToken ct)
        {
            return await _iToDoRepository.GetAllAsync(ct);
        }

        public async Task Update(ToDoItem task, CancellationToken ct)
        {
            await _iToDoRepository.Update(task, ct);
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
        public static int Translator(string stringToTest)
        {
            int taskTextLenght = stringToTest.Length;
            return taskTextLenght;
        }
        #endregion
    }
}
