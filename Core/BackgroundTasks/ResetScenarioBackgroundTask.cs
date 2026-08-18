using FinalProjectCardList.Core.TelegramBot.Scenaries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinalProjectCardList.Core.BackgroundTasks
{
    internal class ResetScenarioBackgroundTask(
       TimeSpan resetScenarioTimeout,
       IScenarioContextRepository scenarioRepository,
       ITelegramBotClient bot)
       : BackgroundTask(TimeSpan.FromHours(1), nameof(ResetScenarioBackgroundTask))
    {
        protected override async Task Execute(CancellationToken ct)
        {
            var contexts = await scenarioRepository.GetContexts(ct);

            foreach (var (userId, context) in contexts)
            {
                if (DateTime.UtcNow - context.CreatedAt < resetScenarioTimeout)
                    continue;

                await scenarioRepository.ResetContext(userId, ct);

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "/addtask", "/show", "/report" }
                })
                {
                    ResizeKeyboard = true
                };

                await bot.SendMessage(
                    chatId: userId,
                    text: $"Сценарий отменен, так как не поступил ответ в течение {resetScenarioTimeout}",
                    replyMarkup: keyboard,
                    cancellationToken: ct);
            }
        }
    }
}
