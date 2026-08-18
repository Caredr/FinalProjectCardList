using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    public interface IScenario
    {
        public bool CanHandle(ScenarioType scenario); // Проверка - могу ли обработать данный сценарий
        public Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct); //Удержание сообщеняи сценария
    }
}
