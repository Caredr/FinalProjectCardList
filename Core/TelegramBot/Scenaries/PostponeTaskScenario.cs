using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot.Dto;
using FinalProjectCardList.Core.TelegramBot.Scenaries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.TelegramBot.Scenarios
{
    internal class PostponeTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _todoService;
        private readonly IToDoListService _todoListService;

        public PostponeTaskScenario(
            IUserService userService,
            IToDoService todoService,
            IToDoListService todoListService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _todoListService = todoListService ?? throw new ArgumentNullException(nameof(todoListService));
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.PostponeTask;
        }

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Update update,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Обработка inline-callback (выбор задачи или списка)
            if (update.CallbackQuery is { } callbackQuery)
            {
                return await HandleCallbackQueryAsync(bot, context, callbackQuery, ct);
            }

            // 2. Обычное сообщение
            if (update.Message is not { } message)
                return ScenarioResult.Completed;

            var user = context.Context as ToDoUser;
            if (user == null)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "❌ Ошибка: пользователь не найден.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            switch (context.CurrentStep)
            {
                // Шаг 0: показываем все активные задачи пользователя
                case null:
                    {
                        var allTasks = await _todoService.GetAllByUserIdAsync(user.UserId, ct);
                        var activeTasks = allTasks.Where(t => t.State == ToDoItemState.Active).ToList();

                        if (activeTasks.Count == 0)
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "У вас нет активных задач для переноса.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var rows = new List<IEnumerable<InlineKeyboardButton>>();

                        foreach (var task in activeTasks.Take(20))
                        {
                            var taskDto = new ToDoItemCallbackDto
                            {
                                Action = "postpone_task",
                                ToDoItemId = task.Id
                            };
                            var callbackData = taskDto.ToString();  // ← Исправлено
                            if (callbackData.Length > 64)
                                callbackData = callbackData[..64];

                            string taskLabel = BuildTaskLabel(task);
                            rows.Add(new[]
                            {
        InlineKeyboardButton.WithCallbackData(taskLabel, callbackData)
    });
                        }

                        var markup = new InlineKeyboardMarkup(rows);

                        await bot.SendMessage(
                            chatId: message.Chat.Id,
                            text: "Выберите задачу для переноса:",
                            replyMarkup: markup,
                            cancellationToken: ct);

                        context.CurrentStep = "SelectTask";
                        return ScenarioResult.Transition;
                    }

                default:
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Неизвестный шаг",
                        cancellationToken: ct);
                    context.CurrentStep = null;
                    context.Data.Clear();
                    return ScenarioResult.Completed;
            }
        }

        private async Task<ScenarioResult> HandleCallbackQueryAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            CallbackQuery callbackQuery,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var data = callbackQuery.Data ?? string.Empty;
            var userId = callbackQuery.From.Id;
            var user = context.Context as ToDoUser;

            if (user == null)
            {
                await bot.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Ошибка: пользователь не найден",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            // Обработка выбора задачи
            if (data.StartsWith("postpone_task"))
            {
                var taskDto = ToDoItemCallbackDto.FromString(data);  // ← Исправлено имя
                var taskId = taskDto.ToDoItemId;

                var task = await _todoService.Get(taskId, ct);
                if (task == null)
                {
                    await bot.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Задача не найдена",
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                // Сохраняем ID задачи
                context.Data["SelectedTaskId"] = taskId;

                // Показываем списки для выбора
                var lists = await _todoListService.GetUserListsAsync(user.UserId, ct);
                var rows = new List<IEnumerable<InlineKeyboardButton>>();

                // Опция "Без списка"
                var noListDto = new ToDoListCallbackDto
                {
                    Action = "postpone_list",
                    ToDoListId = Guid.Empty
                };
                var noListData = ToDoListCallbackDto.ToString(noListDto);
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("📌 Без списка", noListData)
                });

                // Списки пользователя
                foreach (var list in lists)
                {
                    var listDto = new ToDoListCallbackDto  // ← Исправлено имя
                    {
                        Action = "postpone_list",
                        ToDoListId = list.Id
                    };
                    var callbackData = ToDoListCallbackDto.ToString(listDto);  // ← Исправлено
                    if (callbackData.Length > 64)
                        callbackData = callbackData[..64];

                    rows.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(list.Name ?? "(без имени)", callbackData)
                    });
                }

                var markup = new InlineKeyboardMarkup(rows);

                string currentListName = task.ListId != null ? task.ListId.Name : "Без списка";

                await bot.SendMessage(
                    callbackQuery.Message!.Chat.Id,
                    $"Задача: *{task.Name}*\n" +
                    $"Текущий список: _{currentListName}_\n\n" +
                    $"Выберите новый список:",
                    cancellationToken: ct,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: markup);

                context.CurrentStep = "SelectList";
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            // Обработка выбора списка
            if (data.StartsWith("postpone_list"))
            {
                if (!context.Data.TryGetValue("SelectedTaskId", out var taskIdObj) ||
                    taskIdObj is not Guid taskId)
                {
                    await bot.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Ошибка: задача не выбрана",
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                var listDto = ToDoListCallbackDto.FromString(data);  // ← Исправлено имя
                var listId = listDto.ToDoListId;

                ToDoList? targetList = null;
                if (listId != Guid.Empty)
                {
                    targetList = await _todoListService.GetAsync(listId, ct);
                    if (targetList == null)
                    {
                        await bot.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Список не найден",
                            cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }
                }

                // Переносим задачу
                await _todoService.MoveTaskToListAsync(taskId, targetList, ct);

                string listName = targetList?.Name ?? "Без списка";

                await bot.SendMessage(
                    callbackQuery.Message!.Chat.Id,
                    $"✅ Задача перенесена в список: *{listName}*",
                    cancellationToken: ct,
                    parseMode: ParseMode.Markdown);

                await bot.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Задача перенесена!",
                    cancellationToken: ct);

                context.CurrentStep = null;
                context.Data.Clear();
                return ScenarioResult.Completed;
            }

            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
            return ScenarioResult.Transition;
        }

        private static string BuildTaskLabel(ToDoItem task)
        {
            string state = task.State == ToDoItemState.Active ? "[ ]" : "[x]";
            string deadline = task.Deadline.HasValue
                ? $" 📅 {task.Deadline.Value:dd.MM.yyyy}"
                : "";
            string quantity = task.Quantity > 1 ? $" ({task.Quantity}x)" : "";
            string listName = task.ListId != null ? $" [{task.ListId.Name}]" : "";

            string label = $"{state} {task.Name}{deadline}{quantity}{listName}";
            return label.Length > 40 ? label[..40] : label;
        }
    }
}