using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IToDoListRepository // Репозиторий для работы с БД
    {
        //Если спика нет, то возвращает null
        Task<ToDoList?> Get(Guid id, CancellationToken ct);
        Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct); // Получает все списки дел для конкретного пользователя по его идентификатору.
        Task Add(ToDoList list, CancellationToken ct); // Добавляет новый список дел в репозиторий.
        Task Delete(Guid id, CancellationToken ct); // Удаляет список дел по его идентификатору.
        //Проверяет, если ли у пользователя список с таким именем
        Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct);
    }
}
