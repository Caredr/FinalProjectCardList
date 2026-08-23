using FinalProjectCardList.Core.Entities;

namespace FinalProjectCardList.Core.Services
{
    public interface IScryfallService
    {
        Task<ScryfallCard?> GetCardPriceAsync(
         string name,
         string? set = null,
         string? collectorNumber = null,
         CancellationToken ct = default);
    }
}
