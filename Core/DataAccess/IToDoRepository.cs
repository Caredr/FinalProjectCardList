using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IToDoRepository  // Репозиторий для работой с БД по задачам. Содержит методы для получения всех задач пользователя, получения активных задач пользователя, получения задачи по id, добавления новой задачи, обновления существующей задачи и удаления задачи. Также содержит методы для проверки существования задачи с таким именем у пользователя и для получения количества активных задач у пользователя.
    {
        //Возвращает все ToDoItem для UserId
        Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct);
        //Возвращает ToDoItem для UserId со статусом Active
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct);
        // Если задачи нет, то возвращает null
        Task<ToDoItem?> Get(Guid id, CancellationToken ct);
        //Добавляет новую задачу в БД
        Task Add(ToDoItem item, CancellationToken ct);
        //Обновляет существующую задачу в БД. Ищет задачу по id, если не находит, то выбрасывает исключение
        Task Update(ToDoItem item, CancellationToken ct);
        //Удаляет задачу из БД по id. Если задачи нет, то выбрасывает исключение
        Task Delete(Guid id, CancellationToken ct);
        //Проверяет есть ли задача с таким именем у пользователя
        Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct);
        //Возвращает количество активных задач у пользователя
        Task<int> CountActive(Guid userId, CancellationToken ct);
        //Возвращает все ToDoItem для UserId, которые удовлетворяют условию predicate
        Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct);
        //Возвращает активные задачи, у которых Deadline >= from && Deadline < to
        Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetAllAsync(CancellationToken ct);
    }
}
