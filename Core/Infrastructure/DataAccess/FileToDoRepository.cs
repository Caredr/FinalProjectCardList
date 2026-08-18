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
    internal class FileToDoRepository : IToDoRepository
    {

        public FileToDoRepository(string basePath)
        {
            _basePath = Path.Combine(basePath, "Items");
            Directory.CreateDirectory(_basePath);
            _indexPath = Path.Combine(_basePath, "index.json");
            _ = InitializeAsync();
        }
        private readonly string _basePath;
        private readonly string _indexPath;
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        private async Task InitializeAsync() // Создаем индекс
        {
            try
            {
                await EnsureIndexExistsAsync();
            }
            catch (Exception ex)
            {
                // Логируем, но не падаем при старте
                Console.WriteLine($"Index init failed: {ex.Message}");
            }
        }

        private async Task EnsureIndexExistsAsync()
        {
            if (!File.Exists(_indexPath)) // index.json отсутствует?
            {
                var index = new ToDoIndex();    // Создаем пустой
                await ScanAndBuildIndexAsync(index); // Сканируем файлы
                await SaveIndexAsync(index);  // Сохраняем
            }
        }
        private Task ScanAndBuildIndexAsync(ToDoIndex index, CancellationToken ct = default) // СКАНИРОВАНИЕ файловой системы
        {
            var userFolders = Directory.GetDirectories(_basePath);
            foreach (string userFolder in userFolders)
            {
                ct.ThrowIfCancellationRequested();

                if (Guid.TryParse(Path.GetFileName(userFolder), out Guid userId))
                {
                    var itemFiles = Directory.GetFiles(userFolder, "*.json");

                    foreach (string itemFile in itemFiles)
                    {
                        ct.ThrowIfCancellationRequested();

                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(itemFile), out Guid itemId))
                        {
                            index.ItemToUserMap[itemId] = userId;
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
        private string GetFilePath(Guid userId, Guid itemId) => Path.Combine(GetUserFolder(userId), $"{itemId}.json");
        private string GetUserFolder(Guid userId) => Path.Combine(_basePath, userId.ToString());

        private async Task SaveItemAsync(ToDoItem item, CancellationToken ct)
        {
            var userId = item.User.UserId;
            string userFolder = GetUserFolder(userId);
            Directory.CreateDirectory(userFolder);

            string path = GetFilePath(userId, item.Id);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, item, _options, ct);
        }

        private async Task<ToDoItem?> LoadItemAsync(Guid userId, Guid itemId, CancellationToken ct)
        {
            string path = GetFilePath(userId, itemId);
            if (!File.Exists(path)) return null;

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ToDoItem>(stream, _options, ct);
        }

        #region Indexes 
        private async Task<ToDoIndex> LoadIndexAsync()
        {
            if (!File.Exists(_indexPath)) return new ToDoIndex();
            await using var stream = File.OpenRead(_indexPath);
            return await JsonSerializer.DeserializeAsync<ToDoIndex>(stream, _options) ?? new ToDoIndex();
        }

        private async Task SaveIndexAsync(ToDoIndex index, CancellationToken ct = default)
        {
            await using var stream = File.Create(_indexPath);
            await JsonSerializer.SerializeAsync(stream, index, _options, ct);
        }
        #endregion

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
        {
            var items = new List<ToDoItem>();
            string userFolder = GetUserFolder(userId);

            if (!Directory.Exists(userFolder))
                return items.AsReadOnly();

            var files = Directory.GetFiles(userFolder, "*.json");
            foreach (string file in files)
            {
                ct.ThrowIfCancellationRequested();

                var fileName = Path.GetFileNameWithoutExtension(file);
                if (Guid.TryParse(fileName, out Guid itemId))
                {
                    var item = await LoadItemAsync(userId, itemId, ct);
                    if (item != null)
                        items.Add(item);
                }
            }

            return items.AsReadOnly();
        }
        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
        {
            var items = await GetAllByUserId(userId, ct);
            return items.Where(i => i.State == ToDoItemState.Active).ToList().AsReadOnly();
        }
        public async Task<ToDoItem?> Get(Guid itemId, CancellationToken ct = default)
        {
            var index = await LoadIndexAsync();
            if (!index.ItemToUserMap.TryGetValue(itemId, out Guid userId))
                return null;

            string path = GetFilePath(userId, itemId);
            if (!File.Exists(path)) return null;

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ToDoItem>(stream, _options, ct);
        }
        public async Task Add(ToDoItem item, CancellationToken ct = default)
        {
            if (item.Id == Guid.Empty) 
                item.Id = Guid.NewGuid();

            // Сохраняем файл
            string userFolder = GetUserFolder(item.User.UserId);
            Directory.CreateDirectory(userFolder);
            string path = GetFilePath(item.User.UserId, item.Id);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, item, _options, ct);

            // Обновляем индекс
            var index = await LoadIndexAsync();
            index.ItemToUserMap[item.Id] = item.User.UserId;
            await SaveIndexAsync(index);
        }
        public async Task Update(ToDoItem item, CancellationToken ct)
        {
            var existing = await Get(item.Id, ct);
            if (existing == null) throw new InvalidOperationException("Item not found");
            await SaveItemAsync(item, ct);
        }
        public async Task Delete(Guid itemId, CancellationToken ct = default)
        {
            var index = await LoadIndexAsync();
            if (!index.ItemToUserMap.TryGetValue(itemId, out Guid userId))
                return;  // Не найдено

            string path = GetFilePath(userId, itemId);
            if (File.Exists(path)) File.Delete(path);

            // Удаляем из индекса
            index.ItemToUserMap.Remove(itemId);
            await SaveIndexAsync(index);
        }
        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            var items = await GetAllByUserId(userId, ct);
            return items.Any(i => i.Name == name);
        }
        public async Task<int> CountActive(Guid userId, CancellationToken ct)
        {
            var items = await GetActiveByUserId(userId, ct);
            return items.Count;
        }
        public async Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
        {
            var items = await GetAllByUserId(userId, ct);
            return items.Where(predicate).ToList().AsReadOnly();
        }
        public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(
             Guid userId,DateTime from,DateTime to, CancellationToken ct)
        {
            var items = await GetActiveByUserId(userId, ct);
            return items
                .Where(i => i.Deadline >= from && i.Deadline < to).ToList().AsReadOnly();
        }
    }
}
