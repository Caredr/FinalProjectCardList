using LinqToDB;
using LinqToDB.Async;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Infrastructure.DataAccess
{
    internal class SqlUserRepository : IUserRepository
    {
        private readonly DataContextFactory _factory;
        public SqlUserRepository(DataContextFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        public async Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = await dbContext.ToDoUser
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            return model is null
                ? null
                : ModelMapper.MapFromModel(model);
        }
        public async Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = await dbContext.ToDoUser
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

            return model is null
                ? null
                : ModelMapper.MapFromModel(model);
        }

        public async Task Add(ToDoUser user, CancellationToken ct)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();

            var model = ModelMapper.MapToModel(user);

            await dbContext.InsertAsync(model, token: ct);
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
        {
            using var dbContext = ((IDataContextFactory<ToDoDataContext>)_factory).CreateDataContext();
            var models = await dbContext.ToDoUser
                .ToListAsync(ct);
            return models
                .Select(ModelMapper.MapFromModel)
                .ToList();
        }
    }
}