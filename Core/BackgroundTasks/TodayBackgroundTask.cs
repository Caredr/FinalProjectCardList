using FinalProjectCardList.Core.BackgroundTasks;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FinalProjectCardList.Infrastructure.BackgroundTasks;

internal class TodayBackgroundTask : BackgroundTask
{
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IToDoRepository _toDoRepository;

    public TodayBackgroundTask(
        INotificationService notificationService,
        IUserRepository userRepository,
        IToDoRepository toDoRepository)
        : base(TimeSpan.FromDays(1), nameof(TodayBackgroundTask))
    {
        _notificationService = notificationService;
        _userRepository = userRepository;
        _toDoRepository = toDoRepository;
    }

    protected override async Task Execute(CancellationToken ct)
    {
        var users = await _userRepository.GetUsers(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.ToDateTime(TimeOnly.MinValue);
        var to = from.AddDays(1);

        foreach (var user in users)
        {
            ct.ThrowIfCancellationRequested();

            var tasks = await _toDoRepository.GetActiveWithDeadline(
                user.UserId,
                from,
                to,
                ct);

            if (tasks.Count == 0)
                continue;

            var text = BuildText(tasks);

            await _notificationService.ScheduleNotification(
                user.UserId,
                $"Today_{today}",
                text,
                DateTime.UtcNow,
                ct);
        }
    }

    private static string BuildText(IReadOnlyList<ToDoItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Задачи на сегодня:");
        foreach (var task in tasks)
            sb.AppendLine($"• {task.Name}");

        return sb.ToString().TrimEnd();
    }
}