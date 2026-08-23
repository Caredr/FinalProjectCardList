using FinalProjectCardList.Core.BackgroundTasks;
using FinalProjectCardList.Core.DataAccess;
using FinalProjectCardList.Core.Services;

public class PriceBackgroundTask : IBackgroundTask
{
    private readonly IToDoService _todoService;
    private readonly IScryfallService _scryfall;
    private int rateLimit = 100;
    private float hoursDelay = 24;


    public PriceBackgroundTask(IToDoService todoService, IScryfallService scryfall)
    {
        _todoService = todoService;
        _scryfall = scryfall;
    }

    public async Task Start(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var allTasks = await _todoService.GetAllAsync(ct);

            foreach (var task in allTasks.Where(t => !string.IsNullOrEmpty(t.ScryfallSet)))
            {
                var card = await _scryfall.GetCardPriceAsync(
                    task.Name,
                    task.ScryfallSet,
                    task.ScryfallCollectorNumber,
                    ct);

                if (card != null && card.Usd.HasValue)
                {
                    task.LastPriceUsd = card.Usd;
                    task.LastPriceCheckedAt = DateTime.UtcNow;
                    await _todoService.Update(task, ct);
                }

                await Task.Delay(rateLimit, ct); 
            }

            await Task.Delay(TimeSpan.FromHours(hoursDelay), ct);
        }
    }
}