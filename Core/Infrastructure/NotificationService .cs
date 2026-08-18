using LinqToDB;
using LinqToDB.Async;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.DataAccess.Models;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Infrastructure.DataAccess;
using FinalProjectCardList.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Infrastructure
{
    internal class NotificationService : INotificationService
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public NotificationService(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }
        public async Task<bool> ScheduleNotification(Guid userId, string type,
       string text,DateTime scheduledAt, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var exists = await db.Notifications
                .AnyAsync(n => n.UserId == userId && n.Type == type, ct)
                .ConfigureAwait(false);

            if (exists)
            {
                return false;
            }

            var model = new NotificationModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Text = text,
                ScheduledAt = scheduledAt,
                IsNotified = false,
                NotifiedAt = null
            };

            await db.InsertAsync(model, token: ct).ConfigureAwait(false);

            return true;
        }
        public async Task<IReadOnlyList<Notification>> GetScheduledNotification(
             DateTime scheduledBefore,
             CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();

            var models = await db.Notifications
                .Where(n => !n.IsNotified && n.ScheduledAt <= scheduledBefore)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return models
                .Select(MapToDomain)
                .ToList()
                .AsReadOnly();
        }

        public async Task MarkNotified(Guid notificationId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();

            await db.Notifications
                .Where(n => n.Id == notificationId)
                .Set(n => n.IsNotified, true)
                .Set(n => n.NotifiedAt, DateTime.UtcNow)
                .UpdateAsync(ct)
                .ConfigureAwait(false);
        }

        private static Notification MapToDomain(NotificationModel model)
        {
            return new Notification
            {
                Id = model.Id,
                User = null!,
                Type = model.Type,
                Text = model.Text,
                ScheduledAt = model.ScheduledAt,
                IsNotified = model.IsNotified,
                NotifiedAt = model.NotifiedAt
            };
        }
    }
}
