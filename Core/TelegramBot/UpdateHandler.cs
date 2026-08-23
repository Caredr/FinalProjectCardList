using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Entities;

using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot.Dto;
using FinalProjectCardList.Core.TelegramBot.Scenaries;
using FinalProjectCardList.Helpers;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;


namespace FinalProjectCardList.Core.TelegramBot
{
    internal class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _iToDoService;
        private readonly IToDoReportService _iToDoReportService;
        private readonly IEnumerable<IScenario> _scenarios;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly IToDoListService _iToDoListService;
        private readonly int commandDataMaxLenght = 64;
        private readonly int _pageSize = 5;

        /// <summary>
        /// true  — полный доступ ко всем командам.
        /// false — доступны только /start, /help, /report.
        /// </summary>
        private bool commandAccess = true;

        public UpdateHandler(
            IUserService userService,
            IToDoService iToDoService,
            IToDoReportService iToDoReportService,
            IEnumerable<IScenario> scenarios,
            IScenarioContextRepository contextRepository,
            IToDoListService iToDoListService)
        {
            _userService = userService;
            _iToDoService = iToDoService;
            _iToDoReportService = iToDoReportService;
            _scenarios = scenarios;
            _contextRepository = contextRepository;
            _iToDoListService = iToDoListService;
        }



        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            try
            {
                if (update == null)
                    return;
                if (update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync(botClient, update, ct);
                    return;
                }
                if (update.Message?.From == null)
                    return;
                string? commandEater = update.Message.Text?.Trim();
                Guid taskId = default;
                ToDoUser? toDoUser = await _userService.GetUserAsync(update.Message.From.Id, ct);
                ScenarioContext context = await _contextRepository.GetContext(update.Message.From.Id, ct);
                if (commandEater == "/cancel")
                {
                    await _contextRepository.ResetContext(update.Message.From.Id, ct);
                    await SendMainKeyboard(botClient, update.Message.Chat.Id, "Действие отменено.", ct);
                    return;
                }
                if (context != null)
                {
                    await RunScenarioWithKeyboard(botClient, context, update, ct);
                    return;
                }
                if (toDoUser == null)
                {
                    toDoUser = await _userService.RegisterUser(update.Message.From.Id, update.Message.From.Username, ct);
                }
                var userId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id;
                if (userId == null)
                    return;

                // Если доступ ограничен — пропускаем только /start, /help, /report
                if (!commandAccess)
                {
                    bool allowed = commandEater == "/start"
                               || commandEater == "/help"
                               || commandEater == "/report";
                    if (!allowed)
                    {
                        await botClient.SendMessage(
                            update.Message.Chat.Id,
                            "Доступ ограничен. Доступны команды: /start, /help, /report",
                            cancellationToken: ct);
                        return;
                    }
                }

                switch (commandEater)
                {
                    case "/start":
                        await StartPanel(botClient, update, ct);
                        await SendMainKeyboard(botClient, update.Message.Chat.Id, "Главное меню:", ct);
                        break;
                    case "Menu":
                        await botClient.SendMessage(update.Message.Chat, "Доступные команды /start, " +
                            " help, / info, / addtask, / showtasks, / removetask,/ completetask,/ showalltasks,/ report,/ find,/ cansel",
                            cancellationToken: ct);
                        break;
                    case "/show":
                        {
                            // Читаем списки из FileToDoListRepository (файлы на диске)
                            var lists = await _iToDoListService.GetUserListsAsync(toDoUser.UserId, ct);
                            var keyboard = BuildShowListsKeyboard(lists);

                            await botClient.SendMessage(
                                chatId: update.Message.Chat.Id,
                                text: "Выберите список:",
                                replyMarkup: keyboard,
                                cancellationToken: ct);
                            break;
                        }
                    case "/report":
                        {
                            var (total, completed, active, generatedAt) = await _iToDoReportService.GetUserStats(toDoUser.UserId, ct);
                            var text = $"Статистика спискам карт:\n" +
                                       $"- Всего: {total}\n" +
                                       $"- Найдено: {completed}\n" +
                                       $"- Нужно купить: {active}\n" +
                                       $"- Сформировано: {generatedAt:g}";

                            await botClient.SendMessage(chatId: update.Message!.Chat.Id, text: text, cancellationToken: ct);
                            break;
                        }
                    case "/help":
                        await HelpPanel(botClient, update);
                        break;
                    case "/info":
                        await InfoPanel(botClient, update);
                        break;
                    case string s when s.StartsWith("/addtask"):
                        {
                            context = new ScenarioContext(ScenarioType.AddTask);
                            context.Context = toDoUser;
                            await _contextRepository.SetContext(update.Message.From.Id, context, ct);
                            await SendCancelKeyboard(botClient, update.Message.Chat.Id, "Выберите список для задачи:", ct);
                            await ProcessScenario(botClient, context, update, ct);
                            break;
                        }
                    case "/deletetask":
                        {
                            context = new ScenarioContext(ScenarioType.DeleteTask);
                            context.Context = toDoUser;
                            await _contextRepository.SetContext(update.Message.From.Id, context, ct);
                            await SendCancelKeyboard(botClient, update.Message.Chat.Id, "Введите /cancel для отмены.", ct);
                            await RunScenarioWithKeyboard(botClient, context, update, ct);
                            break;
                        }
                    case "addlist":
                        {
                            context = new ScenarioContext(ScenarioType.AddList);
                            context.Context = toDoUser;
                            await _contextRepository.SetContext(update.Message.From.Id, context, ct);
                            // ✅ ДОБАВЛЕНО: кнопка /cancel при старте сценария
                            await SendCancelKeyboard(botClient, update.Message.Chat.Id, "Введите /cancel для отмены.", ct);
                            var scenario = GetScenario(ScenarioType.AddList);
                            var r = await scenario.HandleMessageAsync(botClient, context, update, ct);
                            // ✅ ДОБАВЛЕНО: возврат главной клавиатуры после завершения
                            if (r == ScenarioResult.Completed)
                            {
                                await _contextRepository.ResetContext(update.Message.From.Id, ct);
                                await SendMainKeyboard(botClient, update.Message.Chat.Id, "Готово!", ct);
                            }
                            else
                                await _contextRepository.SetContext(update.Message.From.Id, context, ct);
                            break;
                        }
                    case "deletelist":
                        {
                            context = new ScenarioContext(ScenarioType.DeleteList);
                            context.Context = toDoUser;
                            await _contextRepository.SetContext(update.Message.From.Id, context, ct);
                            // ✅ ДОБАВЛЕНО: кнопка /cancel при старте сценария
                            await SendCancelKeyboard(botClient, update.Message.Chat.Id, "Введите /cancel для отмены.", ct);
                            var scenario = GetScenario(ScenarioType.DeleteList);
                            var r = await scenario.HandleMessageAsync(botClient, context, update, ct);
                            // ✅ ДОБАВЛЕНО: возврат главной клавиатуры после завершения
                            if (r == ScenarioResult.Completed)
                            {
                                await _contextRepository.ResetContext(update.Message.From.Id, ct);
                                await SendMainKeyboard(botClient, update.Message.Chat.Id, "Готово!", ct);
                            }
                            else
                                await _contextRepository.SetContext(update.Message.From.Id, context, ct);
                            break;
                        }
                    case string s when s.StartsWith("/removetask"):
                        {
                            // Извлекаем Guid из подстроки после "/removetask "
                            var idPart = commandEater.Length > "/removetask ".Length
                                ? commandEater["/removetask ".Length..].Trim()
                                : string.Empty;
                            if (Guid.TryParse(idPart, out taskId))
                            {
                                await _iToDoService.DeleteAsync(taskId, ct);
                                await botClient.SendMessage(update.Message.Chat, "Карта удалена", cancellationToken: ct);
                            }
                            else
                            {
                                await botClient.SendMessage(update.Message.Chat,
                                    "Некорректный идентификатор. Используйте: /removetask <guid>",
                                    cancellationToken: ct);
                            }
                            break;
                        }
                    case string si when si.StartsWith("/find"):
                        {
                            // Извлекаем префикс после "/find "
                            var prefix = commandEater.Length > "/find ".Length
                                ? commandEater["/find ".Length..].Trim()
                                : string.Empty;
                            if (string.IsNullOrWhiteSpace(prefix))
                            {
                                await botClient.SendMessage(update.Message.Chat,
                                    "Укажите слово для поиска. Используйте: /find <текст>",
                                    cancellationToken: ct);
                                break;
                            }
                            var found = await _iToDoService.FindAsync(toDoUser, prefix, ct);
                            if (found.Count == 0)
                            {
                                await botClient.SendMessage(update.Message.Chat, "Карты не найдены.", cancellationToken: ct);
                            }
                            else
                            {
                                var sb = new System.Text.StringBuilder("Найдено:");
                                foreach (var t in found)
                                    sb.AppendLine($"\n• {t.Name} [{t.State}]");
                                await botClient.SendMessage(update.Message.Chat, sb.ToString(), cancellationToken: ct);
                            }
                            break;
                        }
                    case string p when p.Equals("/postpone", StringComparison.OrdinalIgnoreCase):
                        {
                            var postponeContext = new ScenarioContext(ScenarioType.PostponeTask)
                            {
                                Context = toDoUser,
                                CurrentStep = null
                            };

                            await _contextRepository.SetContext(
                                update.Message!.From!.Id,
                                postponeContext,
                                ct);

                            var scenario = GetScenario(ScenarioType.PostponeTask);

                            var result = await scenario.HandleMessageAsync(
                                botClient,
                                postponeContext,
                                update,
                                ct);

                            if (result == ScenarioResult.Completed)
                            {
                                await _contextRepository.ResetContext(
                                    update.Message.From.Id,
                                    ct);
                            }
                            else
                            {
                                await _contextRepository.SetContext(
                                    update.Message.From.Id,
                                    postponeContext,
                                    ct);
                            }

                            break;
                        }
                    default:
                        await botClient.SendMessage(update.Message.Chat, "Ошибка, введите доступную команду");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        // Метод обрабатывает все нажатия на инлайн-кнопки от пользователя
        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.CallbackQuery == null)
                return;
            var callbackQuery = update.CallbackQuery;
            var userId = callbackQuery.From.Id;

            var callbackData = callbackQuery.Data ?? string.Empty;
            Console.WriteLine($"[UpdateHandler] Callback: {callbackData}, UserId: {userId}");

            if (callbackData == "addlist")
            {
                var toDoUser = await _userService.GetUserAsync(userId, ct);

                if (toDoUser == null)
                {
                    toDoUser = await _userService.RegisterUser(
                        userId,
                        callbackQuery.From.Username,
                        ct);
                }

                if (toDoUser == null)
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Не удалось зарегистрировать пользователя",
                        cancellationToken: ct);

                    return;
                }

                if (toDoUser.UserId == Guid.Empty)
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "У пользователя некорректный идентификатор",
                        cancellationToken: ct);

                    Console.WriteLine(
                        $"Ошибка: RegisterUser вернул Guid.Empty. TelegramUserId={userId}");

                    return;
                }

                var newContext = new ScenarioContext(ScenarioType.AddList)
                {
                    Context = toDoUser,
                    CurrentStep = "Name"
                };

                await _contextRepository.SetContext(userId, newContext, ct);

                await botClient.SendMessage(
                    callbackQuery.Message!.Chat.Id,
                    "Введите название нового списка карт:",
                    cancellationToken: ct);

                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    cancellationToken: ct);

                return;
            }

            // Пользователь нажал кнопку "Удалить список"
            if (callbackData == "deletelist")
            {
                // Ищем пользователя в базе. Если не найден — регистрируем нового
                var toDoUser = await _userService.GetUserAsync(userId, ct)
                    ?? await _userService.RegisterUser(userId, callbackQuery.From.Username, ct);
                // Создаём новый контекст сценария DeleteList
                var newContext = new ScenarioContext(ScenarioType.DeleteList);
                // Сохраняем пользователя в контексте
                newContext.Context = toDoUser;
                // Получаем экземпляр сценария DeleteList из списка всех сценариев
                await _contextRepository.SetContext(userId, newContext, ct);
                // Запускаем шаг null — сценарий покажет инлайн-список для выбора
                var scenario = GetScenario(ScenarioType.DeleteList);
                // Запускаем шаг null — сценарий покажет инлайн-список для выбора
                await scenario.HandleMessageAsync(botClient, newContext, update, ct);
                // Сохраняем контекст после выполнения шага (шаг мог смениться на "Approve")
                await _contextRepository.SetContext(userId, newContext, ct);
                // Подтверждаем Telegram что callback обработан
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }
            // ─────────────────────────────────────────────
            // Загружаем активный контекст пользователя
            // Нужен для всех сценариев с несколькими шагами (DeleteList, AddTask, DeleteTask)
            // 
            var context = await _contextRepository.GetContext(userId, ct);
            // ─────────────────────────────────────────────
            // БЛОК 1: DeleteList — пользователь выбрал конкретный список для удаления
            // Срабатывает когда активен сценарий DeleteList на шаге "Approve"
            // и данные кнопки начинаются с "deletelist"
            // ─────────────────────────────────────────────
            if (context?.CurrentScenario == ScenarioType.DeleteList && context.CurrentStep == "Approve" && callbackData.StartsWith("deletelist"))
            {
                // Разбираем данные кнопки — внутри зашит Guid выбранного списка
                var dto = ToDoListCallbackDto.FromString(callbackData);
                // Если Guid пустой — данные некорректны, сообщаем об ошибке и выходим
                if (dto.ToDoListId == Guid.Empty)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "Некорректный список", cancellationToken: ct);
                    return;
                }
                // Загружаем список из репозитория по его Guid
                var todoList = await _iToDoListService.GetAsync(dto.ToDoListId, ct);
                // Если список не найден (например уже удалён) — сообщаем об ошибке
                if (todoList == null)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "Список не найден", cancellationToken: ct);
                    return;
                }
                // Кладём выбранный список в Data контекста — сценарий прочитает его на следующем шаге
                context.Data["SelectedList"] = todoList;
                // Получаем сценарий DeleteList
                var scenario = GetScenario(ScenarioType.DeleteList);
                // Вызываем HandleMessageAsync на шаге "Approve" — сценарий отправит кнопки "Да/Нет" и переключит шаг на "Delete"
                var res = await scenario.HandleMessageAsync(botClient, context, update, ct);
                // Если сценарий завершился — сбрасываем контекст пользователя
                if (res == ScenarioResult.Completed)
                    await _contextRepository.ResetContext(userId, ct);
                else
                    // Сохраняем контекст с новым шагом "Delete" чтобы следующий callback (Да/Нет) попал в блок 2
                    await _contextRepository.SetContext(userId, context, ct);
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }
            // ─────────────────────────────────────────────
            // БЛОК 2: DeleteList — пользователь нажал "Да" или "Нет" в подтверждении удаления
            // Срабатывает когда активен сценарий DeleteList на шаге "Delete"
            // ─────────────────────────────────────────────
            if (context?.CurrentScenario == ScenarioType.DeleteList && context.CurrentStep == "Delete")
            {
                // Получаем сценарий DeleteList
                var scenario = GetScenario(ScenarioType.DeleteList);
                // Вызываем HandleMessageAsync на шаге "Delete" — сценарий читает "yes"/"no"
                // и удаляет список или отменяет операцию
                var res = await scenario.HandleMessageAsync(botClient, context, update, ct);
                // Сбрасываем контекст — сценарий завершён
                if (res == ScenarioResult.Completed)
                {
                    await _contextRepository.ResetContext(userId, ct);
                    // Возвращаем пользователя в главное меню
                    await SendMainKeyboard(botClient, callbackQuery.Message!.Chat.Id, "Главное меню:", ct);
                }
                else
                    // Сценарий ещё не завершён — сохраняем обновлённый контек
                    await _contextRepository.SetContext(userId, context, ct);
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }
            // ─────────────────────────────────────────────
            // БЛОК 3: AddTask — пользователь выбрал список при добавлении задачи
            // Срабатывает когда активен сценарий AddTask на шаге "SelectList"
            // ─────────────────────────────────────────────
            if (context?.CurrentScenario == ScenarioType.AddTask &&
             context.CurrentStep == "SelectList" &&
                callbackData.StartsWith("addtask_list"))
            {
                // Убедитесь, что update содержит CallbackQuery
                if (update.CallbackQuery == null)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                    return;
                }

                var scenario = GetScenario(ScenarioType.AddTask);
                var result = await scenario.HandleMessageAsync(botClient, context, update, ct);

                if (result == ScenarioResult.Completed)
                    await _contextRepository.ResetContext(userId, ct);
                else
                    await _contextRepository.SetContext(userId, context, ct);

                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // ─────────────────────────────────────────────
            // БЛОК 4: DeleteTask — пользователь выбрал список (фильтр задач)
            // ─────────────────────────────────────────────
            if (callbackData.StartsWith("deletetask_list"))
            {
                // Загружаем контекст заново (context выше мог быть null)
                var context2 = await _contextRepository.GetContext(userId, ct);
                // Проверяем что активен именно сценарий DeleteTask
                if (context2?.CurrentScenario == ScenarioType.DeleteTask)
                {
                    // Разбираем данные кнопки — внутри зашит Guid выбранного списка
                    var dto = ToDoListCallbackDto.FromString(callbackData);
                    // Сохраняем Id списка в Data контекста — сценарий отфильтрует по нему задачи
                    context2.Data["SelectedListId"] = dto.ToDoListId;
                    // Устанавливаем шаг "SelectList" — сценарий покажет задачи этого списка
                    context2.CurrentStep = "SelectList";
                    // Сохраняем контекст с новым шагом и данными
                    await _contextRepository.SetContext(userId, context2, ct);
                    // Получаем сценарий DeleteTask
                    var sc = GetScenario(ScenarioType.DeleteTask);
                    // Запускаем шаг — сценарий выведет список задач для выбора
                    var res = await sc.HandleMessageAsync(botClient, context2, update, ct);
                    if (res == ScenarioResult.Completed)
                    {
                        // Сбрасываем контекст и показываем главное меню
                        await _contextRepository.ResetContext(userId, ct);
                        await SendMainKeyboard(botClient, callbackQuery.Message!.Chat.Id, "Главное меню:", ct);
                    }
                    else
                        // Сохраняем контекст для следующего шага
                        await _contextRepository.SetContext(userId, context2, ct);
                }
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // ─────────────────────────────────────────────
            // БЛОК 4b: DeleteTask — пользователь выбрал конкретную задачу для удаления
            // ───────────────────────────────────────────── 
            if (callbackData.StartsWith("deletetask_item"))
            {
                // Загружаем контекст пользователя
                var context2 = await _contextRepository.GetContext(userId, ct);
                // Проверяем что активен именно сценарий DeleteTask
                if (context2?.CurrentScenario == ScenarioType.DeleteTask)
                {
                    // Разбираем данные кнопки — внутри зашит Guid выбранной задачи
                    var dto = ToDoListCallbackDto.FromString(callbackData);
                    // Сохраняем Id задачи в Data контекста — шаг "Confirm" прочитает его при удалении
                    context2.Data["SelectedTaskId"] = dto.ToDoListId;
                    // Получаем пользователя из контекста чтобы загрузить его задачи
                    var toDoUser2 = context2.Context;
                    if (toDoUser2 != null)
                    {
                        // Загружаем все активные задачи пользователя
                        var allTasks = await _iToDoService.GetAllByUserIdAsync(toDoUser2.UserId, ct);
                        // Ищем задачу по Id чтобы взять её название для отображения
                        var task = allTasks.FirstOrDefault(t => t.Id == dto.ToDoListId);
                        // Сохраняем название задачи — покажем его в сообщении подтверждения
                        context2.Data["SelectedTaskName"] = task?.Name ?? "задача";
                    }
                    // Устанавливаем шаг "SelectTask" — сценарий отправит кнопки "Да/Нет"
                    context2.CurrentStep = "SelectTask";
                    // Сохраняем контекст
                    await _contextRepository.SetContext(userId, context2, ct);
                    // Получаем сценарий DeleteTask
                    var sc = GetScenario(ScenarioType.DeleteTask);
                    // Запускаем шаг — сценарий выведет "Удалить задачу X?" с кнопками Да/Нет
                    var res = await sc.HandleMessageAsync(botClient, context2, update, ct);
                    if (res == ScenarioResult.Completed)
                    {
                        // Сбрасываем контекст и показываем главное меню
                        await _contextRepository.ResetContext(userId, ct);
                        await SendMainKeyboard(botClient, callbackQuery.Message!.Chat.Id, "Главное меню:", ct);
                    }
                    else
                        // Сохраняем контекст с шагом "Confirm"
                        await _contextRepository.SetContext(userId, context2, ct);
                }
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // ─────────────────────────────────────────────
            // БЛОК 4c: DeleteTask — пользователь нажал "Да" или "Нет" при удалении задачи
            // ─────────────────────────────────────────────
            if (callbackData == "deletetask_yes" || callbackData == "deletetask_no")
            {
                // Загружаем контекст пользователя
                var context2 = await _contextRepository.GetContext(userId, ct);
                // Диагностический лог — выводим в консоль текущий шаг и Id задачи
                // Помогает отследить потерю контекста если удаление не работает
                Console.WriteLine($"[4c] CurrentStep={context2?.CurrentStep}, SelectedTaskId={context2?.Data.GetValueOrDefault("SelectedTaskId")}");
                // Проверяем что активен сценарий DeleteTask И мы находимся на шаге "Confirm"
                if (context2?.CurrentScenario == ScenarioType.DeleteTask
                    && context2.CurrentStep == "Confirm")
                {
                    // Получаем сценарий DeleteTask
                    var sc = GetScenario(ScenarioType.DeleteTask);
                    // Передаём ответ в сценарий — он удалит задачу (yes) или отменит (no)
                    var res = await sc.HandleMessageAsync(botClient, context2, update, ct);
                    if (res == ScenarioResult.Completed)
                    {
                        // Сбрасываем контекст — сценарий завершён
                        await _contextRepository.ResetContext(userId, ct);
                        // Возвращаем пользователя в главное меню
                        await SendMainKeyboard(botClient, callbackQuery.Message!.Chat.Id, "Главное меню:", ct);
                    }
                    else
                        // Сценарий не завершён — сохраняем обновлённый контекст
                        await _contextRepository.SetContext(userId, context2, ct);
                }
                else
                {
                    // Контекст не соответствует ожидаемому — логируем для отладки
                    Console.WriteLine($"[4c] Пропущено: сценарий={context2?.CurrentScenario}, шаг={context2?.CurrentStep}");
                }
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // ─────────────────────────────────────────────
            // БЛОК 5: Показать детали конкретной задачи с кнопками "Выполнить" и "Удалить"
            // ─────────────────────────────────────────────
            if (callbackData.StartsWith("showtask"))
            {
                // Разбираем данные кнопки — внутри зашит Guid задачи
                var dto = ToDoItemCallbackDto.FromString(callbackData);
                // Загружаем задачу из сервиса по её Id
                var item = await _iToDoService.Get(dto.ToDoItemId, ct);
                if (item != null)
                {
                    // Формируем строку статуса: [ ] — активна, [x] — выполнена
                    string state = item.State == ToDoItemState.Active ? "[ ]" : "[x]";
                    // Формируем строку дедлайна, если он задан
                    string deadline = item.Deadline.HasValue ? $"Дедлайн: {item.Deadline.Value:dd.MM.yyyy}" : string.Empty;
                    string quantity = item.Quantity > 1 ? $"\n🔢 Копий: {item.Quantity}" : string.Empty;
                    // Собираем итоговый текст сообщения
                    string text = $"{state} {item.Name}{deadline}{quantity}";
                    // Создаём инлайн-клавиатуру с двумя кнопками действий над задачей
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                         // Кнопка "Выполнить" — передаёт Id задачи в действие completetask
                        InlineKeyboardButton.WithCallbackData("✅ Выполнить",
                            new ToDoItemCallbackDto { Action = "completetask", ToDoItemId = item.Id }.ToString()),
                        // Кнопка "Удалить" — передаёт Id задачи в действие deletetask
                        InlineKeyboardButton.WithCallbackData("❌ Удалить",
                            new ToDoItemCallbackDto { Action = "deletetask", ToDoItemId = item.Id }.ToString())
                    });
                    // Переход от списка к задаче пришёл по нажатию inline-кнопки —
                    // редактируем текущее сообщение, а не отправляем новое
                    await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, text,
                        replyMarkup: keyboard, cancellationToken: ct);
                }
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // ─────────────────────────────────────────────
            // БЛОК 5b: Отметить задачу как выполненную
            // ─────────────────────────────────────────────
            if (callbackData.StartsWith("completetask"))
            {
                // Разбираем данные кнопки — внутри зашит Guid задачи
                var dto = ToDoItemCallbackDto.FromString(callbackData);
                // Загружаем задачу чтобы взять её название для сообщения
                var item = await _iToDoService.Get(dto.ToDoItemId, ct);
                if (item != null)
                {
                    // Помечаем задачу выполненной и сохраняем через репозиторий
                    await _iToDoService.MarkCompletedAsync(dto.ToDoItemId, ct);
                    string quantityText = item.Quantity > 1 ? $" ({item.Quantity}x)" : "";  // Добавить
                    // Сообщаем пользователю об успехе
                    await botClient.SendMessage(callbackQuery.Message!.Chat.Id,
                    $"✅ Задача \"{item.Name}\"{quantityText} выполнена.", cancellationToken: ct);  // Изменено
                }
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // ─────────────────────────────────────────────
            // БЛОК 5c: Удалить задачу напрямую (из кнопки в деталях задачи)
            // Отличие от DeleteTask сценария: удаление без шагов, сразу по нажатию
            // ─────────────────────────────────────────────
            if (callbackData.StartsWith("deletetask"))
            {
                // Разбираем данные кнопки — внутри зашит Guid задачи
                var dto = ToDoItemCallbackDto.FromString(callbackData);
                // Загружаем задачу чтобы взять её название для сообщения
                var item = await _iToDoService.Get(dto.ToDoItemId, ct);
                if (item != null)
                {
                    // Удаляем задачу через сервис
                    await _iToDoService.DeleteAsync(dto.ToDoItemId, ct);
                    string quantityText = item.Quantity > 1 ? $" ({item.Quantity}x)" : "";  
                    // Сообщаем пользователю об успехе
                    await botClient.SendMessage(callbackQuery.Message!.Chat.Id,
                        $"🗑 Задача \"{item.Name}\" {quantityText} удалена.", cancellationToken: ct);
                }
                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }
            // ─────────────────────────────────────────────
            // БЛОК 6b: Показать ВЫПОЛНЕННЫЕ задачи выбранного списка (с пагинацией)
            // Проверяем раньше блока "show", т.к. "show_completed" тоже начинается с "show"
            // ─────────────────────────────────────────────
            if (callbackData.StartsWith("show_completed"))
            {
                await HandleShowCompletedAsync(botClient, callbackQuery, callbackData, ct);
                return;
            }
            // ─────────────────────────────────────────────
            // БЛОК 6: Показать активные задачи выбранного списка (с пагинацией)
            // ─────────────────────────────────────────────
            if (callbackData.StartsWith("show"))
            {
                await HandleShowAsync(botClient, callbackQuery, callbackData, ct);
                return;
            }
            // ─────────────────────────────────────────────
            // Если ни один блок не обработал callback — просто подтверждаем его
            // чтобы убрать индикатор загрузки на кнопке у пользователя
            // ─────────────────────────────────────────────
            // PostponeTask: выбор задачи или списка
            if (context?.CurrentScenario == ScenarioType.PostponeTask)
            {
                Console.WriteLine($"[UpdateHandler] PostponeTask callback: {callbackData}");

                var scenario = GetScenario(ScenarioType.PostponeTask);

                var result = await scenario.HandleMessageAsync(
                    botClient,
                    context,
                    update,
                    ct);

                if (result == ScenarioResult.Completed)
                {
                    await _contextRepository.ResetContext(userId, ct);

                    await SendMainKeyboard(
                        botClient,
                        callbackQuery.Message!.Chat.Id,
                        "Главное меню:",
                        ct);
                }
                else
                {
                    await _contextRepository.SetContext(
                        userId,
                        context,
                        ct);
                }

                // PostponeTaskScenario сам вызывает AnswerCallbackQuery.
                // Поэтому здесь повторно его не вызываем.

                return;
            }

            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        }

        private InlineKeyboardMarkup BuildShowListsKeyboard(IReadOnlyList<ToDoList> lists)
        {
            var rows = new List<IEnumerable<InlineKeyboardButton>>();

            var noListCallbackDto = new ToDoListCallbackDto
            {
                Action = "show",
                ToDoListId = Guid.Empty
            };
            var noListCallback = ToDoListCallbackDto.ToString(noListCallbackDto);

            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData("📌 Без списка", noListCallback)
            ]);

            foreach (var list in lists)
            {
                var dto = new ToDoListCallbackDto
                {
                    Action = "show",
                    ToDoListId = list.Id
                };

                var callbackData = ToDoListCallbackDto.ToString(dto);
                if (callbackData.Length > commandDataMaxLenght)
                    callbackData = callbackData[..commandDataMaxLenght];

                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(list.Name ?? "(без имени)", callbackData)
                });
            }

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🆕 Добавить", "addlist"),
                InlineKeyboardButton.WithCallbackData("❌ Удалить", "deletelist")
            });

            return new InlineKeyboardMarkup(rows);
        }

        // Строит постраничную инлайн-клавиатуру: каждая запись — отдельная кнопка,
        // снизу добавляются стрелки навигации ⬅️/➡️ если страниц больше одной.
        private InlineKeyboardMarkup BuildPagedButtons(
            IReadOnlyList<KeyValuePair<string, string>> callbackData,
            PagedListCallbackDto listDto)
        {
            var totalPages = (int)Math.Ceiling((double)callbackData.Count / _pageSize);
            var page = listDto.Page;

            var pageButtons = callbackData
                .GetBatchByNumber(_pageSize, page)
                .Select(kvp => InlineKeyboardButton.WithCallbackData(kvp.Key, kvp.Value))
                .Select(btn => new[] { btn })
                .ToList();

            var navButtons = new List<InlineKeyboardButton>();
            if (page > 0)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️",
                    new PagedListCallbackDto(listDto.Action, listDto.ToDoListId, page - 1).ToString()));
            if (page < totalPages - 1)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("➡️",
                    new PagedListCallbackDto(listDto.Action, listDto.ToDoListId, page + 1).ToString()));

            var rows = new List<IEnumerable<InlineKeyboardButton>>(pageButtons.Cast<IEnumerable<InlineKeyboardButton>>());
            if (navButtons.Count > 0)
                rows.Add(navButtons);

            return new InlineKeyboardMarkup(rows);
        }

        // Метка задачи для кнопки: статус + имя, обрезанная до разумной длины.
        private static string BuildTaskLabel(int index, ToDoItem task)
        {
            string state = task.State == ToDoItemState.Active ? "[ ]" : "[x]";
            string quantity = task.Quantity > 1 ? $" {task.Quantity}x" : string.Empty;
            string price = task.LastPriceUsd.HasValue ? $" 💰 ${task.LastPriceUsd:N2}" : string.Empty;
            string deadline = task.Deadline.HasValue ? $" 📅 {task.Deadline.Value:dd.MM.yyyy}" : string.Empty;

            string label = $"{index + 1}. {state} {task.Name}{quantity}{price}{deadline}";
            return label.Length > 64 ? label[..64] : label;
        }

        // Показывает активные задачи списка с пагинацией, редактируя текущее сообщение.
        private async Task HandleShowAsync(
    ITelegramBotClient bot,
    CallbackQuery callbackQuery,
    string callbackData,
    CancellationToken ct)
        {
            var userId = callbackQuery.From.Id;

            var toDoUser = await _userService.GetUserAsync(userId, ct)
                ?? await _userService.RegisterUser(userId, callbackQuery.From.Username, ct);

            if (toDoUser == null)
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, "Пользователь не найден", cancellationToken: ct);
                return;
            }

            var listDto = PagedListCallbackDto.FromString(callbackData);

            Guid? listId = listDto.ToDoListId == Guid.Empty ? null : listDto.ToDoListId;

            string listName = "Задачи без списка";

            if (listId.HasValue)
            {
                var list = await _iToDoListService.GetAsync(listDto.ToDoListId, ct);
                listName = list?.Name ?? "Список не найден";
            }

            var tasks = await _iToDoService.GetByUserIdAndList(toDoUser.UserId, listId, ct);

            var activeTasks = tasks.Where(t => t.State == ToDoItemState.Active).ToList();

            long chatId = callbackQuery.Message!.Chat.Id;
            int messageId = callbackQuery.Message.MessageId;

            var showCompletedButton = InlineKeyboardButton.WithCallbackData(
                "✅ Выполненные",
                new PagedListCallbackDto
                {
                    Action = "showcompleted",
                    ToDoListId = listDto.ToDoListId,
                    Page = 0
                }.ToString());

            if (activeTasks.Count == 0)
            {
                var emptyMarkup = new InlineKeyboardMarkup(new[] { new[] { showCompletedButton } });

                await bot.EditMessageText(
                    chatId,
                    messageId,
                    $"{listName}\n\nАктивных задач нет.",
                    replyMarkup: emptyMarkup,
                    cancellationToken: ct);

                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            var pairs = activeTasks
                .Select((task, index) =>
                    new KeyValuePair<string, string>(
                        BuildTaskLabel(index, task),
                        new ToDoItemCallbackDto
                        {
                            Action = "showtask",
                            ToDoItemId = task.Id
                        }.ToString()))
                .ToList();

            var markup = BuildPagedButtons(pairs, listDto);

            var rows = markup.InlineKeyboard.ToList();
            rows.Add(new[] { showCompletedButton });

            var finalMarkup = new InlineKeyboardMarkup(rows);

            await bot.EditMessageText(
                chatId,
                messageId,
                listName,
                replyMarkup: finalMarkup,
                cancellationToken: ct);

            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        }

        // Показывает выполненные задачи списка с пагинацией, редактируя текущее сообщение.
        private async Task HandleShowCompletedAsync(
    ITelegramBotClient bot,
    CallbackQuery callbackQuery,
    string callbackData,
    CancellationToken ct)
        {
            var userId = callbackQuery.From.Id;

            var toDoUser = await _userService.GetUserAsync(userId, ct)
                ?? await _userService.RegisterUser(userId, callbackQuery.From.Username, ct);

            if (toDoUser == null)
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, "Пользователь не найден", cancellationToken: ct);
                return;
            }

            var listDto = PagedListCallbackDto.FromString(callbackData);

            Guid? listId = listDto.ToDoListId == Guid.Empty ? null : listDto.ToDoListId;

            string listName = "Выполненные задачи без списка";

            if (listId.HasValue)
            {
                var list = await _iToDoListService.GetAsync(listDto.ToDoListId, ct);
                listName = list?.Name ?? "Список не найден";
            }

            var tasks = await _iToDoService.GetByUserIdAndList(toDoUser.UserId, listId, ct, ToDoItemState.Completed);

            var completedTasks = tasks.Where(t => t.State == ToDoItemState.Completed).ToList();

            long chatId = callbackQuery.Message!.Chat.Id;
            int messageId = callbackQuery.Message.MessageId;

            var backButton = InlineKeyboardButton.WithCallbackData(
                "⬅️ Назад",
                new PagedListCallbackDto
                {
                    Action = "show",
                    ToDoListId = listDto.ToDoListId,
                    Page = 0
                }.ToString());

            if (completedTasks.Count == 0)
            {
                var emptyMarkup = new InlineKeyboardMarkup(new[] { new[] { backButton } });

                await bot.EditMessageText(
                    chatId,
                    messageId,
                    $"{listName}\n\nВыполненных задач нет.",
                    replyMarkup: emptyMarkup,
                    cancellationToken: ct);

                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            var pairs = completedTasks
                .Select((task, index) =>
                    new KeyValuePair<string, string>(
                        BuildTaskLabel(index, task),
                        new ToDoItemCallbackDto
                        {
                            Action = "showtask",
                            ToDoItemId = task.Id
                        }.ToString()))
                .ToList();

            var markup = BuildPagedButtons(pairs, listDto);

            var rows = markup.InlineKeyboard.ToList();
            rows.Add(new[] { backButton });

            var finalMarkup = new InlineKeyboardMarkup(rows);

            await bot.EditMessageText(
                chatId,
                messageId,
                listName,
                replyMarkup: finalMarkup,
                cancellationToken: ct);

            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        }
        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken ct)
        {
            Console.WriteLine($"[TelegramBot ERROR] Source={source}: {exception}");
            return Task.CompletedTask;
        }

        public async Task StartPanel(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            var user = await _userService.GetUserAsync(update.Message.From.Id, ct);
            if (user == null)
            {
                await _userService.RegisterUser(update.Message.From.Id, update.Message.From.Username, ct);
            }
            await botClient.SendMessage(update.Message.Chat, "Добро пожаловать!");
        }

        public static async Task HelpPanel(ITelegramBotClient botClient, Update update)
        {
            await botClient.SendMessage(update.Message.Chat, " "
                + update.Message.From.Username + " чтобы пользоваться программой" +
                "\n пожалуйста вводите комманды /start, /help, /info, /exit" +
                "\n /start - задает или меняет ваше имя" +
                "\n /help - доска информации" +
                "\n /info - дата создания программы" +
                "\n /addtask - добавить карту" +
                "\n /deletetask - удалить карту (выбор списка → задачи → подтверждение)" +
                "\n /show - показать списки карт (выбери список — активные задачи постранично, можно посмотреть выполненные)" +
                "\n /report - Статистика по картам" +
                "\n /find - Найти по имени" +
                "\n /removetask - удалить карту" +
                "\n /completetask - поставить статус - Completed" +
                "\n /postpone - перенести задачу в другой список" +
                "\n /cancel  - отмена текущего ввода");
        }

        public static async Task InfoPanel(ITelegramBotClient botClient, Update update)
        {
            await botClient.SendMessage(update.Message.Chat, update.Message.From.Username +
                " версия программы - 0.0.8, дата создания 18.11.2025, редактура от 24.06.2026");
        }

        public IScenario GetScenario(ScenarioType scenarioType)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(scenarioType));
            if (scenario == null)
            {
                var availableScenarios = string.Join(", ", _scenarios.Select(s => s.GetType().Name));
                throw new InvalidOperationException(
                    $"Сценарий для типа '{scenarioType}' не найден. " +
                    $"Доступные сценарии: {availableScenarios}");
            }
            return scenario;
        }

        /// <summary>
        /// Запускает сценарий. При завершении возвращает главную клавиатуру.
        /// </summary>
        public async Task RunScenarioWithKeyboard(ITelegramBotClient botClient, ScenarioContext context, Update update, CancellationToken ct)
        {
            IScenario scenario = GetScenario(context.CurrentScenario);
            ScenarioResult result = await scenario.HandleMessageAsync(botClient, context, update, ct);

            if (result == ScenarioResult.Completed)
            {
                await _contextRepository.ResetContext(update.Message.From.Id, ct);
                await SendMainKeyboard(botClient, update.Message.Chat.Id, "Главное меню:", ct);
            }
            else
            {
                // Сценарий ещё идёт — напоминаем про /cancel
                await SendCancelKeyboard(botClient, update.Message.Chat.Id, "Введите /cancel для отмены.", ct);
                await _contextRepository.SetContext(update.Message.From.Id, context, ct);
            }
        }

        public async Task ProcessScenario(ITelegramBotClient botClient, ScenarioContext context, Update update, CancellationToken ct)
        {
            IScenario scenario = GetScenario(context.CurrentScenario);
            ScenarioResult result = await scenario.HandleMessageAsync(botClient, context, update, ct);

            if (result == ScenarioResult.Completed)
                await _contextRepository.ResetContext(update.Message.From.Id, ct);
            else
                await _contextRepository.SetContext(update.Message.From.Id, context, ct);
        }

        private static async Task SendMainKeyboard(ITelegramBotClient bot, long chatId, string text, CancellationToken ct)
        {
            var keyboard = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("/show"),
                        new KeyboardButton("/addtask"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("/report")
                    }
                })
            {
                ResizeKeyboard = true,
                IsPersistent = true  // ← клавиатура остаётся после нажатия кнопки
            };
            await bot.SendMessage(chatId, text, replyMarkup: keyboard, cancellationToken: ct);
        }

        private static async Task SendCancelKeyboard(ITelegramBotClient bot, long chatId, string text, CancellationToken ct)
        {
            var keyboard = new ReplyKeyboardMarkup(new KeyboardButton("/cancel"))
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            await bot.SendMessage(chatId, text, replyMarkup: keyboard, cancellationToken: ct);
        }
    }
}
