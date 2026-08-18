using LinqToDB;
using LinqToDB.Async;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Infrastructure.DataAccess
{
    internal class SqlToDoRepository : IToDoRepository
    {
        private readonly DataContextFactory _factory;

        public SqlToDoRepository(DataContextFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var models = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.User.UserId == userId)
                .ToListAsync(ct);

            return models
                .Select(ModelMapper.MapFromModel)
                .ToList();
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var models = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.User.UserId == userId && i.State != ToDoItemState.Completed)
                .ToListAsync(ct);

            return models
                .Select(ModelMapper.MapFromModel)
                .ToList();
        }

        public async Task<ToDoItem?> Get(Guid id, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            return model is null
                ? null
                : ModelMapper.MapFromModel(model);
        }

        public async Task Add(ToDoItem item, CancellationToken ct)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = ModelMapper.MapToModel(item);

            await dbContext.InsertAsync(model, token: ct);
        }

        public async Task Update(ToDoItem item, CancellationToken ct)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var existing = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .FirstOrDefaultAsync(i => i.Id == item.Id, ct);

            if (existing is null)
                throw new InvalidOperationException($"ToDoItem with id {item.Id} was not found.");

            var updated = ModelMapper.MapToModel(item);

            await dbContext.ToDoItems
                .Where(i => i.Id == item.Id)
                .Set(i => i.Name, updated.Name)
                .Set(i => i.CreatedAt, updated.CreatedAt)
                .Set(i => i.State, updated.State)
                .Set(i => i.StateChangedAt, updated.StateChangedAt)
                .Set(i => i.DeadLine, updated.DeadLine)
                .Set(i => i.User, updated.User)
                .Set(i => i.List, updated.List)
                .UpdateAsync(ct);
        }

        public async Task Delete(Guid id, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var rowsAffected = await dbContext.ToDoItems
                .Where(i => i.Id == id)
                .DeleteAsync(ct);

            if (rowsAffected == 0)
                throw new InvalidOperationException($"ToDoItem with id '{id}' was not found.");
        }

        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            return await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .AnyAsync(i => i.User.UserId == userId && i.Name == name, ct);
        }

        public async Task<int> CountActive(Guid userId, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            return await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .CountAsync(i => i.User.UserId == userId && i.State != ToDoItemState.Completed, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var models = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.User.UserId == userId)
                .ToListAsync(ct);

            return models
                .Select(ModelMapper.MapFromModel)
                .Where(predicate)
                .ToList();
        }
        public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(
        Guid userId,DateTime from,DateTime to,CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var models = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.User.UserId == userId
                            && i.State != ToDoItemState.Completed
                            && i.DeadLine >= from
                            && i.DeadLine < to)
                .ToListAsync(ct);

            return models
                .Select(ModelMapper.MapFromModel)
                .ToList();
        }
    }
}