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
        private readonly IScryfallService _scryfallService;

        public AddTaskScenario(
            IUserService userService,
            IToDoService todoService,
            IToDoListService todoListService,
            IScryfallService scryfallService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _todoListService = todoListService ?? throw new ArgumentNullException(nameof(todoListService));
            _scryfallService = scryfallService ?? throw new ArgumentNullException(nameof(scryfallService));
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

            if (update.CallbackQuery is { } callbackQuery)
            {
                return await HandleCallbackQueryAsync(bot, context, callbackQuery, ct);
            }

            if (update.Message is not { } message)
                return ScenarioResult.Completed;

            var inputText = message.Text?.Trim();
            var user = context.Context as ToDoUser;

            switch (context.CurrentStep)
            {
                case null:
                    {
                        if (user == null)
                        {
                            user = await _userService.RegisterUser(message.Chat.Id, message.From?.Username, ct);
                            context.Context = user;
                        }
                        context.Data["ChatId"] = message.Chat.Id;
                        var lists = await _todoListService.GetUserListsAsync(user.UserId, ct);
                        var rows = new List<IEnumerable<InlineKeyboardButton>>();
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
                            text: "Выберите список для новой карты:",
                            replyMarkup: markup,
                            cancellationToken: ct);

                        context.CurrentStep = "SelectList";
                        return ScenarioResult.Transition;
                    }

                case "SelectList":
                    return ScenarioResult.Transition;

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
                            "Введите название карты:",
                            cancellationToken: ct);

                        context.CurrentStep = "Name";
                        return ScenarioResult.Transition;
                    }

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
                            "Введите код сета (например mkc, 2x2, 4ed) или /skip:",
                            replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("/skip"))
                            {
                                ResizeKeyboard = true,
                                OneTimeKeyboard = true
                            },
                            cancellationToken: ct);

                        context.CurrentStep = "Set";
                        return ScenarioResult.Transition;
                    }

                case "Set":
                    {
                        if ((inputText ?? string.Empty).Equals("/skip", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Data["ScryfallSet"] = null;
                            context.Data["ScryfallCollectorNumber"] = null;
                            await CreateTaskAsync(bot, context, user!, ct);
                            return ScenarioResult.Completed;
                        }

                        context.Data["ScryfallSet"] = inputText;

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Введите collector number:",
                            cancellationToken: ct);

                        context.CurrentStep = "CollectorNumber";
                        return ScenarioResult.Transition;
                    }

                case "CollectorNumber":
                    {
                        context.Data["ScryfallCollectorNumber"] = inputText;
                        await CreateTaskAsync(bot, context, user!, ct);
                        return ScenarioResult.Completed;
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

        private async Task CreateTaskAsync(
    ITelegramBotClient bot,
    ScenarioContext context,
    ToDoUser user,
    CancellationToken ct)
        {
            var taskName = context.Data["TaskName"] as string;
            var scryfallSet = context.Data.TryGetValue("ScryfallSet", out var setObj) ? setObj as string : null;
            var scryfallCollectorNumber = context.Data.TryGetValue("ScryfallCollectorNumber", out var numObj) ? numObj as string : null;
            var quantity = context.Data.TryGetValue("Quantity", out var qObj) && qObj is int q ? q : 1;

            Console.WriteLine($"[CreateTaskAsync] Task: {taskName}, Set: {scryfallSet}, Number: {scryfallCollectorNumber}");

            ScryfallCard? card = null;
            if (!string.IsNullOrEmpty(scryfallSet) && !string.IsNullOrEmpty(scryfallCollectorNumber))
            {
                card = await _scryfallService.GetCardPriceAsync(
                    taskName,
                    scryfallSet,
                    scryfallCollectorNumber,
                    ct);

                Console.WriteLine($"[CreateTaskAsync] Scryfall API: {card?.Name}, Usd: {card?.Usd}");
            }
            else if (!string.IsNullOrEmpty(taskName))
            {
                card = await _scryfallService.GetCardPriceAsync(taskName, null, null, ct);
            }

            ToDoList? list = null;
            if (context.Data.TryGetValue("SelectedListId", out var listIdObj) &&
                listIdObj is Guid listId &&
                listId != Guid.Empty)
            {
                list = await _todoListService.GetAsync(listId, ct);
            }

            var item = await _todoService.AddAsync(
                user,
                taskName,
                list,
                DateTime.MaxValue,
                quantity,
                ct);

            Console.WriteLine($"[CreateTaskAsync] After AddAsync: {item.Id}, ListId: {item.ListId?.Id}");

            if (card != null && card.Usd.HasValue)
            {
                Console.WriteLine($"[CreateTaskAsync] Before Update: {item.Name}, Usd: {item.LastPriceUsd}");

                item.ScryfallSet = scryfallSet;
                item.ScryfallCollectorNumber = scryfallCollectorNumber;
                item.LastPriceUsd = card.Usd;
                item.LastPriceCheckedAt = DateTime.UtcNow;

                Console.WriteLine($"[CreateTaskAsync] After assignment: Usd={item.LastPriceUsd}, Set={item.ScryfallSet}, Number={item.ScryfallCollectorNumber}");

                await _todoService.Update(item, ct);

                Console.WriteLine($"[CreateTaskAsync] After Update call: {item.Name}, Usd: {item.LastPriceUsd}");
            }
            else
            {
                Console.WriteLine($"[CreateTaskAsync] Skip Update: card={card?.Name}, Usd={card?.Usd}");
            }

            string cardInfo = !string.IsNullOrEmpty(scryfallSet) && !string.IsNullOrEmpty(scryfallCollectorNumber)
                ? $" [{scryfallSet} #{scryfallCollectorNumber}]"
                : "";

            string priceText = card?.Usd != null ? $"\n💰 Цена: ${card.Usd:N2}" : "";
            string quantityText = quantity > 1 ? $"\nКоличество: {quantity}x" : "";

            if (!context.Data.TryGetValue("ChatId", out var chatIdObj) || chatIdObj is not long chatId)
            {
                Console.WriteLine("[CreateTaskAsync] ChatId not found in context.Data");
                return;
            }

            await bot.SendMessage(
                chatId,
                $"✅ Карта создана!" +
                $"\n{taskName}{cardInfo}" +
                quantityText +
                priceText,
                cancellationToken: ct);

            Console.WriteLine($"[CreateTaskAsync] Message sent to {chatId}");

            context.CurrentStep = null;
            context.Data.Clear();
        }
    }
}