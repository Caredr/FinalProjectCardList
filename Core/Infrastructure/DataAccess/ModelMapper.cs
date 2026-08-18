using FinalProjectCardList.Core.DataAccess.Models;
using FinalProjectCardList.Core.Entities;


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
                Name = model.Name,
                CreatedAt = model.CreatedAt,
                State = (ToDoItemState)model.State,
                StateChangedAt = model.StateChangedAt,
                Deadline = model.DeadLine,
                User = MapFromModel(model.User),
                List = MapFromModel(model.List)
            };
        }

        public static ToDoItemModel MapToModel(ToDoItem entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new ToDoItemModel
            {
                Id = entity.Id,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                State = entity.State,   // ПОПРАВКА: одно преобразование
                StateChangedAt = entity.StateChangedAt,
                DeadLine = entity.Deadline,
                User = MapToModel(entity.User),
                List = MapToModel(entity.List)
            };
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
