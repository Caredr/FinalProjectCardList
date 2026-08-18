using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Services
{
    internal interface IUserService // Сервис для работы с бизнес-логикой, связанной с пользователями. Он использует IUserRepository для взаимодействия с данными и предоставляет методы для регистрации новых пользователей и получения информации о существующих пользователях.
    {
        // Регистрирует нового пользователя в системе. Принимает TelegramUserId и TelegramUserName, создает новый объект ToDoUser и сохраняет его в базе данных. Возвращает зарегистрированного пользователя.
        Task<ToDoUser> RegisterUser(long telegramUserId, string telegramUserName, CancellationToken ct);
        // Получает информацию о пользователе по его TelegramUserId. Принимает TelegramUserId и возвращает объект ToDoUser, если пользователь найден, или null, если пользователь не найден.
        Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken ct);
    }
}
