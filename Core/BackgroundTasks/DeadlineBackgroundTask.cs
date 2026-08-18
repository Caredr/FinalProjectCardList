using FinalProjectCardList.Core.BackgroundTasks;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinalProjectCardList.Infrastructure.BackgroundTasks;

internal class DeadlineBackgroundTask : BackgroundTask
{
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IToDoRepository _toDoRepository;
    public DeadlineBackgroundTask(
        INotificationService notificationService,
        IUserRepository userRepository,
        IToDoRepository toDoRepository)
        : base(TimeSpan.FromHours(1), nameof(DeadlineBackgroundTask))
    {
        _notificationService = notificationService;
        _userRepository = userRepository;
        _toDoRepository = toDoRepository;
    }
    protected override async Task Execute(CancellationToken ct)
    {
        var users = await _userRepository.GetUsers(ct);

        foreach (var user in users)
        {
            ct.ThrowIfCancellationRequested();
            var tasks = await _toDoRepository.GetActiveWithDeadline(
                user.UserId,
                DateTime.UtcNow.AddDays(-1).Date,
                DateTime.UtcNow.Date,
                ct);
            foreach (var task in tasks)
            {
                ct.ThrowIfCancellationRequested();
                await _notificationService.ScheduleNotification(
                    user.UserId,
                    $"Deadline_{task.Id}",
                    $"Ой! Вы пропустили дедлайн по задаче {task.Name}",
                    DateTime.UtcNow,
                    ct);
            }
        }
    }
}