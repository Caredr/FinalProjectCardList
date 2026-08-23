using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    public interface IToDoService                   
    {
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);
        Task<ToDoItem> AddAsync(ToDoUser user, string name, ToDoList? list, DateTime deadLine, int quantity, CancellationToken ct);
        Task MarkCompletedAsync(Guid id, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct, ToDoItemState? stateFilter = null); 
        Task<IReadOnlyList<ToDoList>> GetListsByUserId(Guid userId, CancellationToken ct);
        Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct);
        Task MoveTaskToListAsync(Guid taskId, ToDoList? targetList, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetAllAsync(CancellationToken ct);
        Task Update(ToDoItem task, CancellationToken ct);
    }
}
