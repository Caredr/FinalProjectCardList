using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{

    internal class ShowTasksScenario : IScenario
    {
        private readonly IToDoService _todoService;
        private readonly IUserService _userService;

        public ShowTasksScenario(IToDoService todoService, IUserService userService)
        {
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.ShowTasks;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Update update,
            CancellationToken ct)
        {
            long chatId;
            long telegramUserId;

            if (update.CallbackQuery is { } cq)
            {
                chatId = cq.Message!.Chat.Id;
                telegramUserId = cq.From.Id;
            }
            else if (update.Message is { } msg)
            {
                chatId = msg.Chat.Id;
                telegramUserId = msg.From!.Id;
            }
            else
            {
                return ScenarioResult.Completed;
            }
            if (context.Context is not ToDoUser user)
            {
                user = await _userService.GetUserAsync(telegramUserId, ct)
                    ?? await _userService.RegisterUser(telegramUserId, null, ct);
                context.Context = user;
            }
            Guid? listId = null;
            if (context.Data.TryGetValue("SelectedListId", out var listIdObj)
                && listIdObj is Guid lid && lid != Guid.Empty)
                listId = lid;

            string listName = context.Data.TryGetValue("SelectedListName", out var nameObj) && nameObj is string n
                ? n : "Без списка";
            var tasks = await _todoService.GetByUserIdAndList(user.UserId, listId, ct);

            if (tasks.Count == 0)
            {
                await bot.SendMessage(chatId, $"Список: {listName}\n\nЗадач пока нет.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }
            await bot.SendMessage(chatId, $"Список: {listName} ({tasks.Count} задач(и)):", cancellationToken: ct);
            foreach (var task in tasks)
            {
                string state = task.State == ToDoItemState.Active ? "[ ]" : "[x]";
                string deadline = task.Deadline.HasValue
                    ? $"\nДедлайн: {task.Deadline.Value:dd.MM.yyyy}"
                    : string.Empty;

                string text = $"{state} {task.Name}{deadline}";

                var showDto = new ToDoItemCallbackDto { Action = TaskActionScenario.ShowTask.ToAction(), ToDoItemId = task.Id };

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Выполнить",
                        new ToDoItemCallbackDto { Action = TaskActionScenario.CompleteTask.ToAction(), ToDoItemId = task.Id }.ToString()),
                    InlineKeyboardButton.WithCallbackData("❌ Удалить",
                        new ToDoItemCallbackDto { Action = TaskActionScenario.DeleteTask.ToAction(), ToDoItemId = task.Id }.ToString())
                });

                await bot.SendMessage(chatId, text, replyMarkup: keyboard, cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}
