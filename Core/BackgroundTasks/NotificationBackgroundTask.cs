using FinalProjectCardList.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.BackgroundTasks
{
    internal class NotificationBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly ITelegramBotClient _bot;

        public NotificationBackgroundTask(
            INotificationService notificationService,
            ITelegramBotClient bot)
            : base(TimeSpan.FromMinutes(1), nameof(NotificationBackgroundTask))
        {
            _notificationService = notificationService;
            _bot = bot;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var notifications = await _notificationService
                .GetScheduledNotification(DateTime.UtcNow, ct)
                .ConfigureAwait(false);

            foreach (var notification in notifications)
            {
                if (notification.User == null || notification.User.TelegramUserId <= 0)
                {
                    continue;
                }

                await _bot.SendMessage(
                    chatId: notification.User.TelegramUserId,
                    text: notification.Text,
                    cancellationToken: ct).ConfigureAwait(false);

                await _notificationService
                    .MarkNotified(notification.Id, ct)
                    .ConfigureAwait(false);
            }
        }
    }
}
