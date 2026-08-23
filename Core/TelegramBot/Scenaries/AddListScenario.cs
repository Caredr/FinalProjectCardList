using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    internal class AddListScenario: IScenario
    {
        private IUserService _iUserService;
        private IToDoListService _iToDoListService;
        public AddListScenario(IUserService iUserService, IToDoListService iToDoListService)
        {
            _iUserService = iUserService ?? throw new ArgumentNullException(nameof(iUserService));
            _iToDoListService = iToDoListService ?? throw new ArgumentNullException(nameof(iToDoListService));
        }
        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.AddList;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, 
            ScenarioContext context, Update update, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var user = context.Context!;
            var inputText = update.Message?.Text?.Trim();
            switch (context.CurrentStep)
            {
                case null:
                    var todoUser = await _iUserService.GetUserAsync(update.Message.From.Id, ct);
                    context.Context = todoUser;
                    await bot.SendMessage(update?.Message?.Chat.Id,"Введите название списка:",
                        cancellationToken: ct);
                    context.CurrentStep = "Name";
                    return ScenarioResult.Transition;
                case "Name":
                    if (update?.Message?.Text == null)
                        return ScenarioResult.Completed;
                    var todoUserInName = context.Context;
                    if (todoUserInName == null)
                        return ScenarioResult.Completed;
                    var name = update?.Message?.Text;
                    try
                    {
                        await _iToDoListService.AddAsync(todoUserInName, name, ct);
                    await bot.SendMessage(update?.Message?.Chat.Id, "Список создан!", cancellationToken: ct);
                    context.CurrentStep = null;
                    return ScenarioResult.Completed;
                    }
                    catch (Exception ex)
                    {
                        await bot.SendMessage(update.Message.Chat.Id,
                            $"Ошибка при создании списка: {ex.Message}",
                            cancellationToken: ct);
                        context.CurrentStep = null;  
                        return ScenarioResult.Completed;
                    }
                default:
                    await bot.SendMessage(update?.Message?.Chat.Id, "Неизвестный шаг", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
}
