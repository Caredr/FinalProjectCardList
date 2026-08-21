using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    internal class AddTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _todoService;
        private readonly IToDoListService _todoListService;

        public AddTaskScenario(
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
            return scenario == ScenarioType.AddTask;
        }

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Update update,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Обработка inline‑callback (выбор списка)
            if (update.CallbackQuery is { } callbackQuery)
            {
                return await HandleCallbackQueryAsync(bot, context, callbackQuery, ct);
            }

            // 2. Обычное сообщение
            if (update.Message is not { } message)
                return ScenarioResult.Completed;

            var inputText = message.Text?.Trim();
            var user = context.Context;

            switch (context.CurrentStep)
            {
                // Шаг 0: показываем списки
                case null:
                    {
                        if (user == null)
                        {
                            user = await _userService.RegisterUser(message.Chat.Id, message.From?.Username, ct);
                            context.Context = user;
                        }

                        var lists = await _todoListService.GetUserListsAsync(user.UserId, ct);

                        var rows = new List<IEnumerable<InlineKeyboardButton>>();

                        // 📌 Без списка
                        var noListDto = new ToDoListCallbackDto
                        {
                            Action = "addtask_list",
                            ToDoListId = Guid.Empty
                        };
                        var noListData = ToDoListCallbackDto.ToString(noListDto);
                        rows.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📌Без списка", noListData)
                        });

                        // Списки пользователя
                        foreach (var list in lists)
                        {
                            var dto = new ToDoListCallbackDto
                            {
                                Action = "addtask_list",
                                ToDoListId = list.Id
                            };
                            var callbackData = ToDoListCallbackDto.ToString(dto);
                            if (callbackData.Length > 64)
                                callbackData = callbackData[..64];

                            rows.Add(new[]
                            {
                                InlineKeyboardButton.WithCallbackData(list.Name ?? "(без имени)", callbackData)
                            });
                        }

                        var markup = new InlineKeyboardMarkup(rows);

                        await bot.SendMessage(
                            chatId: message.Chat.Id,
                            text: "Выберите список для новой задачи:",
                            replyMarkup: markup,
                            cancellationToken: ct);

                        context.CurrentStep = "SelectList";
                        return ScenarioResult.Transition;
                    }

                case "Quantity":
                    {
                        if (string.IsNullOrWhiteSpace(inputText))
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "Количество не может быть пустым!",
                                cancellationToken: ct);
                            return ScenarioResult.Transition;
                        }

                        if (!int.TryParse(inputText, out var quantity) || quantity <= 0)
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "❌ Введите положительное число!",
                                cancellationToken: ct);
                            return ScenarioResult.Transition;
                        }

                        context.Data["Quantity"] = quantity;

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Введите название задачи:",
                            cancellationToken: ct);

                        context.CurrentStep = "Name";
                        return ScenarioResult.Transition;
                    }

                // Шаг 1: ввод имени
                case "Name":
                    {
                        if (string.IsNullOrWhiteSpace(inputText))
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "Название не может быть пустым!",
                                cancellationToken: ct);
                            return ScenarioResult.Transition;
                        }

                        context.Data["TaskName"] = inputText;

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Введите дедлайн (ДД.ММ.ГГГГ) или /skip для без дедлайна:",
                            cancellationToken: ct);

                        context.CurrentStep = "Deadline";
                        return ScenarioResult.Transition;
                    }

                // Шаг 2: дедлайн + фактическое создание задачи
                case "Deadline":
                    {
                        if (!context.Data.TryGetValue("TaskName", out var taskNameObj) ||
                            taskNameObj is not string taskName ||
                            string.IsNullOrWhiteSpace(taskName))
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "❌ Ошибка: название задачи потеряно. Начните заново.",
                                cancellationToken: ct);

                            context.CurrentStep = null;
                            context.Data.Clear();
                            return ScenarioResult.Completed;
                        }

                        if (user == null)
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "❌ Ошибка: пользователь не найден. Начните заново.",
                                cancellationToken: ct);

                            context.CurrentStep = null;
                            context.Data.Clear();
                            return ScenarioResult.Completed;
                        }

                        var quantity =
                            context.Data.TryGetValue("Quantity", out var qObj) && qObj is int q
                                ? q
                                : 1;

                        // Восстанавливаем выбранный список
                        ToDoList? list = null;

                        if (context.Data.TryGetValue("SelectedListId", out var listIdObj) &&
                            listIdObj is Guid listId &&
                            listId != Guid.Empty)
                        {
                            list = await _todoListService.GetAsync(listId, ct);
                        }

                        Console.WriteLine(
                            $"[Deadline] SelectedListId = {(context.Data.TryGetValue("SelectedListId", out var lid) ? lid.ToString() : "null")}");
                        Console.WriteLine(
                            $"[Deadline] list = {(list?.Id.ToString() ?? "null")}");

                        if (DateTime.TryParseExact(
                                inputText ?? string.Empty,
                                "dd.MM.yyyy",
                                null,
                                System.Globalization.DateTimeStyles.None,
                                out var deadline))
                        {
                            var item = await _todoService.AddAsync(
                                user,
                                taskName,
                                list,
                                deadline,
                                quantity,
                                ct);

                            Console.WriteLine(
                                $"[Deadline] Задача создана: {item.Id}, ListId = {item.ListId}");

                            await bot.SendMessage(
                                message.Chat.Id,
                                $"✅ *{taskName}*\n" +
                                $"📅 Дедлайн: `{deadline:dd.MM.yyyy}`\n" +
                                $"🆔 `{item.Id}`",
                                cancellationToken: ct,
                                parseMode: ParseMode.Markdown);

                            context.CurrentStep = null;
                            context.Data.Clear();
                            return ScenarioResult.Completed;
                        }

                        if ((inputText ?? string.Empty)
                            .Equals("/skip", StringComparison.OrdinalIgnoreCase))
                        {
                            var item = await _todoService.AddAsync(
                                user,
                                taskName,
                                list,
                                DateTime.MaxValue,
                                quantity,
                                ct);

                            Console.WriteLine(
                                $"[Deadline] Задача создана (без дедлайна): {item.Id}, ListId = {item.ListId}");

                            await bot.SendMessage(
                                message.Chat.Id,
                                $"✅ *{taskName}* добавлена без дедлайна!\n" +
                                $"🆔 `{item.Id}`",
                                cancellationToken: ct,
                                parseMode: ParseMode.Markdown);

                            context.CurrentStep = null;
                            context.Data.Clear();
                            return ScenarioResult.Completed;
                        }

                        await bot.SendMessage(
                            message.Chat.Id,
                            "❌ Неверный формат!\n" +
                            "💡 Пример: `15.12.2024`\n" +
                            "💡 Или `/skip` для без дедлайна",
                            cancellationToken: ct,
                            parseMode: ParseMode.Markdown);

                        return ScenarioResult.Transition;
                    }

                case "SelectList":
                    // В этом шаге работаем только через CallbackQuery
                    return ScenarioResult.Transition;

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

        /// <summary>
        /// Обработка callback'ов внутри AddTaskScenario (выбор списка).
        /// </summary>
        private async Task<ScenarioResult> HandleCallbackQueryAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            CallbackQuery callbackQuery,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var data = callbackQuery.Data ?? string.Empty;

            Console.WriteLine(
                $"[HandleCallbackQueryAsync] CurrentStep = {context.CurrentStep}, Data = {data}");

            if (context.CurrentStep != "SelectList")
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            var dto = ToDoListCallbackDto.FromString(data);

            Console.WriteLine(
                $"[HandleCallbackQueryAsync] Action = {dto.Action}, ToDoListId = {dto.ToDoListId}");

            if (dto.Action != "addtask_list")
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            if (dto.ToDoListId == Guid.Empty)
            {
                Console.WriteLine("[HandleCallbackQueryAsync] Выбрано 'Без списка'");
                context.Data.Remove("SelectedListId");
            }
            else
            {
                var list = await _todoListService.GetAsync(dto.ToDoListId, ct);

                if (list == null)
                {
                    Console.WriteLine(
                        $"[HandleCallbackQueryAsync] Список не найден: {dto.ToDoListId}");

                    await bot.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Список не найден",
                        cancellationToken: ct);

                    return ScenarioResult.Transition;
                }

                Console.WriteLine(
                    $"[HandleCallbackQueryAsync] Сохраняем SelectedListId = {dto.ToDoListId}");

                context.Data["SelectedListId"] = dto.ToDoListId;
            }

            var chatId = callbackQuery.Message!.Chat.Id;

            await bot.SendMessage(
                chatId,
                "Введите количество:",
                cancellationToken: ct);

            context.CurrentStep = "Quantity";

            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
            return ScenarioResult.Transition;
        }
    }
}