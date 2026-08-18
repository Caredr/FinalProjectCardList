using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IToDoListService // Сервис для работы с бизнес-логикой,
                                        // связанной со списками дел.
                                        // Он использует IToDoListRepository для взаимодействия с данными и предоставляет методы
                                        // для создания, получения, удаления и получения списков дел для пользователей.
    {
        Task<ToDoList> AddAsync(ToDoUser user, string name, CancellationToken ct); // Метод для создания нового списка дел. Принимает пользователя, которому принадлежит список, и имя списка.
        Task<ToDoList?> GetAsync(Guid id, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<ToDoList>> GetUserListsAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken ct);
    }
}
