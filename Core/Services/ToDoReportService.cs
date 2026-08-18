using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Infrastructure.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Services
{
    internal class ToDoReportService : IToDoReportService
    {
        private readonly IToDoRepository _iToDoRepository;
        public ToDoReportService(IToDoRepository inMemoryToDoRepository)
        {
            _iToDoRepository = inMemoryToDoRepository;
        }
        public async Task<(int total, int completed, int active, DateTime generatedAt)>  GetUserStats(Guid userId, CancellationToken ct)
        {
            int total = 0;
            int completed = 0;
            int active = 0;
            DateTime generatedAt;
            List<ToDoItem> items = (List<ToDoItem>)await _iToDoRepository.GetActiveByUserId(userId, ct);
            total = items.Count();
            items = (List<ToDoItem>)await _iToDoRepository.GetActiveByUserId(userId, ct);
            active = items.Count;
            completed = total - active;
            generatedAt = DateTime.UtcNow;
            return (total, completed, active, generatedAt);
        }
    }
}
