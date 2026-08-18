using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess
{
    public interface IToDoReportService // Сервис для получения статистики по задачам пользователя.
                                        // Содержит метод для получения статистики,
                                        // который возвращает общее количество задач,
                                        // количество выполненных задач, количество активных задач и дату генерации отчета.
    {
        public Task <(int total, int completed, int active, DateTime generatedAt)> GetUserStats(Guid userId, CancellationToken ct);
    }
}
