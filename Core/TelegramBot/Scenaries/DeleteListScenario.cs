using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;
using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    internal class DeleteListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _todoListService;

        public DeleteListScenario(IUserService userService, IToDoListService todoListService)
        {
            _userService = userService;
            _todoListService = todoListService;
        }
        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.DeleteList;
        }
        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot,
        ScenarioContext context, Update update, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            long chatId = update.CallbackQuery?.Message?.Chat.Id
                       ?? update.Message?.Chat.Id
                       ?? 0;
            long telegramUserId = update.CallbackQuery?.From.Id
                               ?? update.Message?.From?.Id
                               ?? 0;

            switch (context.CurrentStep)
            {
                case null:
                {
                    var todoUser = context.Context
                        ?? await _userService.GetUserAsync(telegramUserId, ct);
                    if (todoUser == null)
                        return ScenarioResult.Completed;
                    context.Context = todoUser;

                    var lists = await _todoListService.GetUserListsAsync(todoUser.UserId, ct);
                    if (lists.Count == 0)
                    {
                        await bot.SendMessage(chatId, "У вас нет списков для удаления.", cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }

                    var buttons = new List<InlineKeyboardButton[]>();
                    foreach (var list in lists)
                    {
                        var dto = new ToDoListCallbackDto
                        {
                            Action = TaskActionScenario.DeleteList.ToAction(),
                            ToDoListId = list.Id
                        };
                        var cb = ToDoListCallbackDto.ToString(dto);
                        if (cb.Length > 64) cb = cb[..64];
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name ?? "(без имени)", cb) });
                    }

                    var markup = new InlineKeyboardMarkup(buttons);
                    await bot.SendMessage(chatId, "Выберите список для удаления:",
                        replyMarkup: markup,
                        cancellationToken: ct);

                    context.CurrentStep = "Approve";
                    return ScenarioResult.Transition;
                }

                case "Approve":
                {
                    if (!context.Data.TryGetValue("SelectedList", out var obj) || obj is not ToDoList todoList)
                        return ScenarioResult.Transition; 

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Да", "yes"),
                        InlineKeyboardButton.WithCallbackData("❌ Нет", "no")
                    });

                    await bot.SendMessage(chatId,
                        $"Подтверждаете удаление списка \"{todoList.Name}\" и всех его задач?",
                        replyMarkup: keyboard,
                        cancellationToken: ct);

                    context.CurrentStep = "Delete";
                    return ScenarioResult.Transition;
                }

                case "Delete":
                {
                    var data = update.CallbackQuery?.Data;
                    if (data == null)
                        return ScenarioResult.Transition;

                    if (!context.Data.TryGetValue("SelectedList", out var obj) || obj is not ToDoList todoList)
                        return ScenarioResult.Completed;

                    if (data == "yes")
                    {
                        await _todoListService.DeleteAsync(todoList.Id, ct);
                        await bot.SendMessage(chatId, "✅ Список удалён.", cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(chatId, "Удаление отменено.", cancellationToken: ct);
                    }

                    context.Data.Remove("SelectedList");
                    context.CurrentStep = null;
                    return ScenarioResult.Completed;
                }

                default:
                    context.CurrentStep = null;
                    return ScenarioResult.Completed;
            }
        }

        public async Task<ScenarioResult> HandleCallbackQueryAsync(ITelegramBotClient bot, ScenarioContext context,
       CallbackQuery callbackQuery, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (context.CurrentStep != "Delete")
                return ScenarioResult.Completed;
            var data = callbackQuery.Data;
            if (!context.Data.TryGetValue("User", out var userObj) || userObj is not ToDoUser todoUser)
                return ScenarioResult.Completed;
            if (!context.Data.TryGetValue("SelectedList", out var obj) || obj is not ToDoList todoList)
                return ScenarioResult.Completed;
            if (data == "yes")
            {
                await _todoListService.DeleteAsync(todoList.Id, ct);
                await bot.SendMessage(callbackQuery.Message!.Chat.Id,
                    "Список и все его задачи удалены.",
                    cancellationToken: ct);
            }
            else if (data == "no")
            {
                await bot.SendMessage(
                    callbackQuery.Message!.Chat.Id,
                    "Удаление отменено.",
                    cancellationToken: ct);
            }

            context.Data.Remove("SelectedList");
            context.CurrentStep = null;
            return ScenarioResult.Completed;
        }
    }
}
