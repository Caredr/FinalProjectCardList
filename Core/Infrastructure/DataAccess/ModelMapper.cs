using FinalProjectCardList.Core.DataAccess.Models;
using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;

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

            if (model.User == null)
                throw new InvalidOperationException("User не может быть null для ToDoItem");

            var entity = new ToDoItem
            {
                Id = model.Id,
                Name = model.Name,
                UserId = MapFromModel(model.User),
                ListId = model.List != null ? MapFromModel(model.List) : null,
                CreatedAt = model.CreatedAt,
                State = model.State,
                StateChangedAt = model.StateChangedAt,
                Deadline = model.Deadline,
                Quantity = model.Quantity
            };

            return entity;
        }

        public static ToDoItemModel MapToModel(ToDoItem entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var userId = entity.UserId.UserId;
            var listId = entity.ListId?.Id;

            var model = new ToDoItemModel
            {
                Id = entity.Id,
                Name = entity.Name,
                UserId = userId,
                ListId = listId,
                CreatedAt = entity.CreatedAt,
                State = entity.State,
                StateChangedAt = entity.StateChangedAt,
                Deadline = entity.Deadline,
                Quantity = entity.Quantity,
                // Навигационные свойства
                User = entity.UserId != null ? MapToModel(entity.UserId) : null,
                List = entity.ListId != null ? MapToModel(entity.ListId) : null
            };

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
                User = model.User != null ? MapFromModel(model.User) : null
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
                User = entity.User != null ? MapToModel(entity.User) : null
            };
        }
    }
}