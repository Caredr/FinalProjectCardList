using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IToDoService // Сервис для работы с бизнес-логикой,
                                    // связанной с задачами. Он использует IToDoRepository для взаимодействия
                                    // с данными и предоставляет методы для создания, получения, обновления и удаления задач,
                                    // а также для получения задач по определенным критериям (например, по имени или по списку дел).
    {
        //Ищет ToDoItem для User по имени, начинающемуся с namePrefix. Возвращает список найденных ToDoItem.
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct);
        //Возвращает все ToDoItem для UserId, отсортированные по CreatedAt в порядке убывания.
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);
        //Возвращает ToDoItem для UserId со статусом Active
        Task<ToDoItem> AddAsync(ToDoUser user, string name, ToDoList? list, DateTime deadLine, CancellationToken ct);
        // Обновляет существующую задачу. Ищет задачу по id, если не находит, то выбрасывает исключение. Если находит, то обновляет имя, дедлайн и список дел задачи.
        Task MarkCompletedAsync(Guid id, CancellationToken ct);
        // Удаляет задачу по id. Если задачи нет, то выбрасывает исключение
        Task DeleteAsync(Guid id, CancellationToken ct);
        // Возвращает все ToDoItem для UserId и ListId (если ListId не null), отсортированные по CreatedAt в порядке убывания.
        Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct, ToDoItemState? stateFilter = null); 
        // Получаем информацию о Id пользователя и информацию о листе которым он сейчас пользуется

        // Возвращает уникальные ToDoList из задач пользователя (читает из файлов, не из памяти)
        Task<IReadOnlyList<ToDoList>> GetListsByUserId(Guid userId, CancellationToken ct);
        Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct);
    }
}
