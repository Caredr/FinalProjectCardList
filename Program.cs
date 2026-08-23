using FinalProjectCardList.Core.BackgroundTasks;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Exeptions;
using FinalProjectCardList.Core.Infrastructure;
using FinalProjectCardList.Core.Infrastructure.DataAccess;
using FinalProjectCardList.Core.Services;
using FinalProjectCardList.Core.TelegramBot;
using FinalProjectCardList.Core.TelegramBot.Scenaries;
using FinalProjectCardList.Core.TelegramBot.Scenarios;
using FinalProjectCardList.Infrastructure.BackgroundTasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;


namespace FinalProjectCardList
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            try
            {
                // ← Загрузка настроек из appsettings.json
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var telegramBotToken = config["TelegramBotToken"];
                var connectionStringData = config["DatabaseConnection"];

                CancellationTokenSource sourceToken = new();
                CancellationToken token = sourceToken.Token;

                var botClient = new TelegramBotClient(telegramBotToken);  // ← Используем из конфига

                string connectionString = connectionStringData;  // ← Используем из конфига

                DataContextFactory factory = new DataContextFactory(connectionString);

                IUserRepository userRepo = new SqlUserRepository(factory);
                IToDoRepository toDoRepo = new SqlToDoRepository(factory);
                IToDoListRepository toDoListRepo = new SqlToDoListRepository(factory);
                INotificationService notificationService = new NotificationService(factory);
                IScryfallService scryfallService = new ScryfallService();

                UserService userService = new UserService(userRepo);
                ToDoReportService toDoReportService = new ToDoReportService(toDoRepo);
                ToDoService toDoService = new ToDoService(toDoRepo);
                ToDoListService toDoListService = new ToDoListService(toDoListRepo, toDoService, userRepo);

                var scenarios = new List<IScenario>
                {
                    new AddTaskScenario(userService, toDoService, toDoListService, scryfallService),
                    new AddListScenario(userService, toDoListService),
                    new DeleteListScenario(userService, toDoListService),
                    new ShowTasksScenario(toDoService, userService),
                    new DeleteTaskScenario(userService, toDoService, toDoListService),
                    new PostponeTaskScenario(userService, toDoService, toDoListService)
                };

                InMemoryScenarioContextRepository contextRepo = new();

                var updateHandler = new UpdateHandler(
                    userService,
                    toDoService,
                    toDoReportService,
                    scenarios,
                    contextRepo,
                    toDoListService);

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
                    DropPendingUpdates = true
                };

                var backgroundTaskRunner = new BackgroundTaskRunner();

                backgroundTaskRunner.AddTask(new ResetScenarioBackgroundTask(resetScenarioTimeout: TimeSpan.FromHours(1),
                scenarioRepository: contextRepo, bot: botClient));

                backgroundTaskRunner.AddTask(new NotificationBackgroundTask( notificationService: notificationService,
                    bot: botClient));
                backgroundTaskRunner.AddTask(new DeadlineBackgroundTask( notificationService: notificationService,
                          userRepository: userRepo, toDoRepository: toDoRepo));
                backgroundTaskRunner.AddTask(new TodayBackgroundTask(notificationService: notificationService,
                    userRepository: userRepo, toDoRepository: toDoRepo));

                backgroundTaskRunner.AddTask(new PriceBackgroundTask(
                        todoService: toDoService,
                                 scryfall: scryfallService));
                backgroundTaskRunner.StartTasks(token);

                botClient.StartReceiving(
                    updateHandler.HandleUpdateAsync,
                    updateHandler.HandleErrorAsync,
                    receiverOptions,
                    token);



                await botClient.SetMyCommands(new[]
                {
                    new BotCommand { Command = "start", Description = "Запуск и главное меню" },
                    new BotCommand { Command = "help", Description = "Справка по командам" },
                    new BotCommand { Command = "info", Description = "Информация о программе" },
                    new BotCommand { Command = "addtask", Description = "Добавить задачу" },
                    new BotCommand { Command = "deletetask", Description = "Удалить задачу" },
                    new BotCommand { Command = "show", Description = "Списки и задачи (с выполненными)" },
                    new BotCommand { Command = "report", Description = "Статистика по задачам" },
                    new BotCommand { Command = "find", Description = "Поиск задач по имени" },
                    new BotCommand { Command = "postpone", Description = "Перенести задачу в другой список" },
                    new BotCommand { Command = "cancel", Description = "Отмена текущего ввода" },
                }, cancellationToken: token);

                var me = await botClient.GetMe();
                Console.WriteLine($"{me.FirstName} запущен!");
                Console.WriteLine("Нажмите A чтобы остановиться");

                if (Console.ReadLine() == "A")
                {
                    await backgroundTaskRunner.StopTasks(CancellationToken.None);
                    sourceToken.Cancel();
                    Environment.Exit(0);
                }

                await Task.Delay(-1, token);
            }
            catch (TypeInitializationException ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка {ex.Message}");
                Console.WriteLine($"StackTrace:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine("Inner exception: {0}", ex.InnerException);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка {ex.Message}");
                Console.WriteLine($"StackTrace:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine("Inner exception: {0}", ex.InnerException);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка {ex.Message}");
            }
            catch (TaskCountLimitException ex)
            {
                Console.WriteLine($"Превышен лимит карт {ex.Message}");
            }
            catch (TaskLengthLimitException ex)
            {
                Console.WriteLine($"Превышен лимит длины названия карты {ex.Message}");
            }
            catch (DuplicateTaskException ex)
            {
                Console.WriteLine($"Такое название карты уже есть {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка {ex.Message}");
                Console.WriteLine($"Произошла непредвиденная ошибка: {ex.GetType().Name}");
                Console.WriteLine($"StackTrace:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine("Inner exception: {0}", ex.InnerException);
            }
        }
    }
}