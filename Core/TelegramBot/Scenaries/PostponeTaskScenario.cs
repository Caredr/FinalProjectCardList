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

            if (update.CallbackQuery is { } callbackQuery)
            {
                return await HandleCallbackQueryAsync(bot, context, callbackQuery, ct);
            }

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
                            var callbackData = taskDto.ToString();  
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

            Console.WriteLine($"[PostponeTaskScenario] Callback: {data}, Step: {context.CurrentStep}");

            if (user == null)
            {
                await bot.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Ошибка: пользователь не найден",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            var callbackTaskDto = ToDoItemCallbackDto.FromString(data);

            if (callbackTaskDto.Action == "postpone_task")
            {
                Console.WriteLine("[PostponeTaskScenario] Выбор задачи");

                var taskId = callbackTaskDto.ToDoItemId;

                Console.WriteLine($"[PostponeTaskScenario] TaskId: {taskId}");

                var task = await _todoService.Get(taskId, ct);
                if (task == null)
                {
                    Console.WriteLine($"[PostponeTaskScenario] Задача не найдена: {taskId}");
                    await bot.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Задача не найдена",
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                context.Data["SelectedTaskId"] = taskId;
                Console.WriteLine($"[PostponeTaskScenario] Сохранено SelectedTaskId: {taskId}");

                var lists = await _todoListService.GetUserListsAsync(user.UserId, ct);
                Console.WriteLine($"[PostponeTaskScenario] Найдено списков: {lists.Count}");

                var rows = new List<IEnumerable<InlineKeyboardButton>>();

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

                foreach (var list in lists)
                {
                    var listDto = new ToDoListCallbackDto
                    {
                        Action = "postpone_list",
                        ToDoListId = list.Id
                    };
                    var listCallbackData = ToDoListCallbackDto.ToString(listDto);
                    if (listCallbackData.Length > 64)
                        listCallbackData = listCallbackData[..64];

                    rows.Add(new[]
                    {
        InlineKeyboardButton.WithCallbackData(list.Name ?? "(без имени)", listCallbackData)
    });
                }

                var markup = new InlineKeyboardMarkup(rows);

                string currentListName = task.ListId?.Name ?? "Без списка";

                Console.WriteLine($"[PostponeTaskScenario] Отправляем списки, текущий: {currentListName}");

                await bot.SendMessage(
                    callbackQuery.Message!.Chat.Id,
                    $"Задача: *{task.Name}*\n" +
                    $"Текущий список: _{currentListName}_\n\n" +
                    $"Выберите новый список:",
                    cancellationToken: ct,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: markup);

                context.CurrentStep = "SelectList";
                Console.WriteLine($"[PostponeTaskScenario] Установлен шаг: {context.CurrentStep}");

                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            var callbackListDto = ToDoListCallbackDto.FromString(data);

            if (callbackListDto.Action == "postpone_list")
            {
                Console.WriteLine("[PostponeTaskScenario] Выбор списка");

                if (!context.Data.TryGetValue("SelectedTaskId", out var taskIdObj) ||
                    taskIdObj is not Guid taskId)
                {
                    Console.WriteLine("[PostponeTaskScenario] Задача не выбрана");
                    await bot.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Ошибка: задача не выбрана",
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                var listId = callbackListDto.ToDoListId;

                Console.WriteLine($"[PostponeTaskScenario] ListId: {listId}");

                ToDoList? targetList = null;
                if (listId != Guid.Empty)
                {
                    targetList = await _todoListService.GetAsync(listId, ct);
                    if (targetList == null)
                    {
                        Console.WriteLine($"[PostponeTaskScenario] Список не найден: {listId}");
                        await bot.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Список не найден",
                            cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }
                }

                await _todoService.MoveTaskToListAsync(taskId, targetList, ct);
                Console.WriteLine($"[PostponeTaskScenario] Задача {taskId} перенесена в {targetList?.Name ?? "Без списка"}");

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

            Console.WriteLine($"[PostponeTaskScenario] Неизвестный callback: {data}");
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