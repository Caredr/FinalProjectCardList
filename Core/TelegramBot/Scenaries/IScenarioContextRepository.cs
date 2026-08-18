using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    internal interface IScenarioContextRepository // Сохраняет состояние диалога Telegram бота между сообщениями.
                                                  // Без этого бот забывает, на каком шаге находится пользователь.
    {
        Task<ScenarioContext?> GetContext(long userId, CancellationToken ct); // Загружает состояние диалога пользователя из БД/файлов/Redis
        Task SetContext(long userId, ScenarioContext context, CancellationToken ct); // Сериализует ScenarioContext → JSON/BSON, Записывает в хранилище по ключу userId,Перезаписывает существующее состояние 
        Task ResetContext(long userId, CancellationToken ct); // Удаляет запись из хранилища
        Task<IReadOnlyList<KeyValuePair<long, ScenarioContext>>> GetContexts(CancellationToken ct);
    }
}
