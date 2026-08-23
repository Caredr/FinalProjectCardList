using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Infrastructure.DataAccess
{
    internal class FileUserRepository : IUserRepository
    {
        private readonly string _basePath;
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
        public FileUserRepository(string basePath)
        {
            _basePath = basePath; 
            Directory.CreateDirectory(_basePath);
        }
        public  Task<string> GetFilePath(Guid userId) => Task.FromResult(Path.Combine(_basePath, $"{userId}.json"));
        public async Task<ToDoUser?> LoadUserAsync(Guid userId, CancellationToken ct)
        {
            string path =await GetFilePath(userId);
            if (!File.Exists(path)) return null;
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ToDoUser>(stream, _options, ct);
        }
        public async Task SaveUserAsync(ToDoUser user, CancellationToken ct)
        {
            string path = await GetFilePath(user.UserId);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, user, _options, ct);
        }
        public async Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct)
        {
            return await LoadUserAsync(userId, ct);
        }
        public async Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct)
        {
            var files = Directory.GetFiles(_basePath, "*.json");
            foreach (string file in files)
            {
                ct.ThrowIfCancellationRequested();
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out Guid id))
                {
                    var user = await LoadUserAsync(id, ct);
                    if (user?.TelegramUserId == telegramUserId)
                        return user;
                }
            }
            return null;
        }
        public async Task Add(ToDoUser user, CancellationToken ct)
        {
            if (user.UserId == Guid.Empty) 
                user.UserId = Guid.NewGuid();
            await SaveUserAsync(user, ct);
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
        {
            var users = new List<ToDoUser>();

            if (!Directory.Exists(_basePath))
                return users.AsReadOnly();

            var files = Directory.GetFiles(_basePath, "*.json");

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                await using var stream = File.OpenRead(file);
                var user = await JsonSerializer.DeserializeAsync<ToDoUser>(stream, _options, ct);

                if (user != null)
                    users.Add(user);
            }

            return users.AsReadOnly();
        }
    }
}

