using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    internal class DeleteTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _todoService;
        private readonly IToDoListService _todoListService;

        public DeleteTaskScenario(
            IUserService userService,
            IToDoService todoService,
            IToDoListService todoListService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _todoListService = todoListService ?? throw new ArgumentNullException(nameof(todoListService));
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteTask;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Update update,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            long chatId = update.CallbackQuery?.Message?.Chat.Id
                       ?? update.Message?.Chat.Id
                       ?? 0;

            long telegramUserId = update.CallbackQuery?.From.Id
                               ?? update.Message?.From?.Id
                               ?? 0;

            if (context.Context is not ToDoUser user)
            {
                user = await _userService.GetUserAsync(telegramUserId, ct)
                    ?? await _userService.RegisterUser(telegramUserId, null, ct);
                context.Context = user;
            }

            switch (context.CurrentStep)
            {
                case null:
                    {
                        var lists = await _todoListService.GetUserListsAsync(user.UserId, ct);
                        var rows = new List<IEnumerable<InlineKeyboardButton>>();

                        var noListDto = new ToDoListCallbackDto { Action = TaskActionScenario.DeleteTaskList.ToAction(), ToDoListId = Guid.Empty };
                        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📌 Без списка", ToDoListCallbackDto.ToString(noListDto)) });

                        foreach (var list in lists)
                        {
                            var dto = new ToDoListCallbackDto { Action = TaskActionScenario.DeleteTaskList.ToAction(), ToDoListId = list.Id };
                            var cb = ToDoListCallbackDto.ToString(dto);
                            if (cb.Length > 64) cb = cb[..64];
                            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name ?? "(без имени)", cb) });
                        }

                        await bot.SendMessage(chatId,
                            "Выберите список задачи для удаления:",
                            replyMarkup: new InlineKeyboardMarkup(rows),
                            cancellationToken: ct);

                        context.CurrentStep = "SelectList";
                        return ScenarioResult.Transition;
                    }

                case "SelectList":
                    {
                        if (!context.Data.TryGetValue("SelectedListId", out var lidObj) || lidObj is not Guid listId)
                            return ScenarioResult.Transition;

                        Guid? nullableListId = listId == Guid.Empty ? null : listId;
                        var tasks = await _todoService.GetByUserIdAndList(user.UserId, nullableListId, ct);

                        if (tasks.Count == 0)
                        {
                            await bot.SendMessage(chatId, "В этом списке нет задач.", cancellationToken: ct);
                            context.CurrentStep = null;
                            return ScenarioResult.Completed;
                        }

                        var rows = new List<IEnumerable<InlineKeyboardButton>>();
                        foreach (var task in tasks)
                        {
                            string state = task.State == ToDoItemState.Active ? "[ ]" : "[x]";
                            string label = $"{state} {task.Name}";
                            if (label.Length > 40) label = label[..40];

                            var dto = new ToDoListCallbackDto { Action = TaskActionScenario.DeleteTaskItem.ToAction(), ToDoListId = task.Id };
                            var cb = ToDoListCallbackDto.ToString(dto);
                            if (cb.Length > 64) cb = cb[..64];
                            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, cb) });
                        }

                        await bot.SendMessage(chatId,
                            "Выберите задачу для удаления:",
                            replyMarkup: new InlineKeyboardMarkup(rows),
                            cancellationToken: ct);

                        context.CurrentStep = "SelectTask";
                        return ScenarioResult.Transition;
                    }

                case "SelectTask":
                    {
                        if (!context.Data.TryGetValue("SelectedTaskId", out var tidObj) || tidObj is not Guid taskId)
                            return ScenarioResult.Transition;

                        string taskName = context.Data.TryGetValue("SelectedTaskName", out var nameObj) && nameObj is string n
                            ? n : "задачу";

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Да", "deletetask_yes"),
                            InlineKeyboardButton.WithCallbackData("❌ Нет", "deletetask_no")
                        });

                        await bot.SendMessage(chatId,
                            $"Удалить задачу \"{taskName}\"?",
                            replyMarkup: keyboard,
                            cancellationToken: ct);

                        context.CurrentStep = "Confirm";
                        return ScenarioResult.Transition;
                    }

                case "Confirm":
                    {
                        var cbData = update.CallbackQuery?.Data ?? string.Empty;

                        if (cbData == "deletetask_yes")
                        {
                            if (!context.Data.TryGetValue("SelectedTaskId", out var tidObj) || tidObj is not Guid taskId)
                            {
                                await bot.SendMessage(chatId, "❌ Ошибка: задача не найдена в контексте. Начните заново.", cancellationToken: ct);
                                context.CurrentStep = null;
                                context.Data.Clear();
                                return ScenarioResult.Completed;
                            }

                            await _todoService.DeleteAsync(taskId, ct);
                            string taskName = context.Data.TryGetValue("SelectedTaskName", out var n) && n is string name
                                ? name : "Задача";
                            await bot.SendMessage(chatId, $"✅ \"{taskName}\" удалена.", cancellationToken: ct);
                        }
                        else
                        {
                            await bot.SendMessage(chatId, "Удаление отменено.", cancellationToken: ct);
                        }

                        context.CurrentStep = null;
                        context.Data.Clear();
                        return ScenarioResult.Completed;
                    }

                default:
                    context.CurrentStep = null;
                    return ScenarioResult.Completed;
            }
        }
    }
}
