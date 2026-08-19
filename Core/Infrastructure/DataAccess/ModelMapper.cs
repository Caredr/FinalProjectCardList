using FinalProjectCardList.Core.DataAccess.Models;
using FinalProjectCardList.Core.Entities;
using System.Reflection;


namespace FinalProjectCardList.Core.Infrastructure.DataAccess
{
    internal static class ModelMapper
    {
        public static ToDoUser MapFromModel(ToDoUserModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new ToDoUser
            {
                UserId = model.UserId,
                TelegramUserName = model.TelegramUserName,
                RegisteredAt = model.RegisteredAt,
                TelegramUserId = model.TelegramUserId
            };
        }

        public static ToDoUserModel MapToModel(ToDoUser entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new ToDoUserModel
            {
                UserId = entity.UserId,
                TelegramUserName = entity.TelegramUserName,
                RegisteredAt = entity.RegisteredAt,
                TelegramUserId = entity.TelegramUserId
            };
        }

        public static ToDoItem MapFromModel(ToDoItemModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new ToDoItem
            {
                Id = model.Id,
                UserId = model.UserId,
                ListId = model.ListId,
                Name = model.Name,
                CreatedAt = model.CreatedAt,
                State = (ToDoItemState)model.State,
                StateChangedAt = model.StateChangedAt,
                Deadline = model.Deadline,   // колонка DeadLine в БД
                Quantity = model.Quantity
            };
        }

        public static ToDoItemModel MapToModel(ToDoItem entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var model = new ToDoItemModel
            {
                Id = entity.Id,
                Name = entity.Name,
                UserId = entity.Id,
                ListId = entity.ListId,          // достаточно Id,
                CreatedAt = entity.CreatedAt,
                State = entity.State,
                StateChangedAt = entity.StateChangedAt,
                Deadline = entity.Deadline,
                Quantity = entity.Quantity
            };
            // если список есть — маппим навигационное свойство, если нет — оставляем null
            if (entity.User != null)
                model.User = MapToModel(entity.User);
            if (entity.ListId != null)
                model.List = MapToModel(entity.List);

            return model;
        }

        public static ToDoList MapFromModel(ToDoListModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new ToDoList
            {
                Id = model.Id,
                Name = model.Name,
                CreatedAt = model.CreatedAt,
                UserId = model.UserId,
                User = MapFromModel(model.User)
            };
        }

        public static ToDoListModel MapToModel(ToDoList entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new ToDoListModel
            {
                Id = entity.Id,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                UserId = entity.UserId,
                User = MapToModel(entity.User)
            };
        }
    }
}
