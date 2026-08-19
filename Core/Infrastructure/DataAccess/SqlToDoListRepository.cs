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
    internal class SqlToDoListRepository : IToDoListRepository
    {
        private readonly DataContextFactory _factory;

        public SqlToDoListRepository(DataContextFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = await dbContext.ToDoList
                .LoadWith(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            return model is null
                ? null
                : ModelMapper.MapFromModel(model);
        }

        public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var models = await dbContext.ToDoList
                .LoadWith(i => i.User)
                .Where(i => i.UserId == userId)
                .ToListAsync(ct);

            return models
                .Select(ModelMapper.MapFromModel)
                .ToList();
        }

        public async Task Add(ToDoList list, CancellationToken ct)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = ModelMapper.MapToModel(list);

            await dbContext.InsertAsync(model, token: ct);
        }

        public async Task Delete(Guid id, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var rowsAffected = await dbContext.ToDoList
                .Where(i => i.Id == id)
                .DeleteAsync(ct);

            if (rowsAffected == 0)
                throw new InvalidOperationException($"ToDoList with id '{id}' was not found.");
        }

        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            return await dbContext.ToDoList
                .LoadWith(i => i.User)
                .AnyAsync(i => i.UserId == userId && i.Name == name, ct);
        }
    }
}