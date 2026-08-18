using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using System.Text.Json;

namespace FinalProjectCardList.Core.Infrastructure.DataAccess
{
    /// <summary>
    /// Хранит списки задач в файловой системе.
    /// Каждый список — отдельный JSON-файл: data/lists/{userId}/{listId}.json
    /// </summary>
    internal class FileToDoListRepository : IToDoListRepository
    {
        private readonly string _basePath;
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public FileToDoListRepository(string basePath)
        {
            _basePath = Path.Combine(basePath, "Lists");
            Directory.CreateDirectory(_basePath);
        }

        private string GetUserFolder(Guid userId) =>
            Path.Combine(_basePath, userId.ToString());

        private string GetFilePath(Guid userId, Guid listId) =>
            Path.Combine(GetUserFolder(userId), $"{listId}.json");

        public async Task Add(ToDoList list, CancellationToken ct)
        {
            var folder = GetUserFolder(list.UserId);
            Directory.CreateDirectory(folder);
            var path = GetFilePath(list.UserId, list.Id);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, list, _options, ct);
        }

        public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            foreach (var userFolder in Directory.GetDirectories(_basePath))
            {
                var path = Path.Combine(userFolder, $"{id}.json");
                if (!File.Exists(path)) continue;
                await using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<ToDoList>(stream, _options, ct);
            }
            return null;
        }

        public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
        {
            var result = new List<ToDoList>();
            var folder = GetUserFolder(userId);
            if (!Directory.Exists(folder))
                return result.AsReadOnly();
            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                ct.ThrowIfCancellationRequested();
                await using var stream = File.OpenRead(file);
                var list = await JsonSerializer.DeserializeAsync<ToDoList>(stream, _options, ct);
                if (list != null)
                    result.Add(list);
            }
            return result.AsReadOnly();
        }

        public async Task Delete(Guid id, CancellationToken ct)
        {
            foreach (var userFolder in Directory.GetDirectories(_basePath))
            {
                var path = Path.Combine(userFolder, $"{id}.json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return;
                }
            }
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            var lists = await GetByUserId(userId, ct);
            return lists.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
