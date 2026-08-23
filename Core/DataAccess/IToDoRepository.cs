using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    internal interface IToDoRepository  
    {
        Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct);
        Task<ToDoItem?> Get(Guid id, CancellationToken ct);
        Task Add(ToDoItem item, CancellationToken ct);
        Task Update(ToDoItem item, CancellationToken ct);
        Task Delete(Guid id, CancellationToken ct);
        Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct);
        Task<int> CountActive(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetAllAsync(CancellationToken ct);
    }
}
