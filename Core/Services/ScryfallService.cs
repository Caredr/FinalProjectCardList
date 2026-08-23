using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Services
{
    internal class ScryfallService : IScryfallService
    {

        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.scryfall.com/")
        };

        public async Task<ScryfallCard?> GetCardPriceAsync(
            string name,
            string? set = null,
            string? collectorNumber = null,
            CancellationToken ct = default)
        {
            string url;

            if (!string.IsNullOrEmpty(set) && !string.IsNullOrEmpty(collectorNumber))
            {
                url = $"cards/search?q=set%3A{set}+number%3A{collectorNumber}";
            }
            else
            {
                url = $"cards/named?exact={Uri.EscapeDataString(name)}";
            }

            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("object", out var obj) && obj.GetString() == "error")
                return null;

            ScryfallCard card = new ScryfallCard();

            if (root.TryGetProperty("name", out var nameProp))
                card.Name = nameProp.GetString() ?? string.Empty;

            if (root.TryGetProperty("set", out var setProp))
                card.Set = setProp.GetString() ?? string.Empty;

            if (root.TryGetProperty("collector_number", out var numProp))
                card.CollectorNumber = numProp.GetString() ?? string.Empty;

            if (root.TryGetProperty("prices", out var prices))
            {
                if (prices.TryGetProperty("usd", out var usdProp) && usdProp.ValueKind == JsonValueKind.String)
                    card.Usd = decimal.Parse(usdProp.GetString() ?? "0");

                if (prices.TryGetProperty("usd_foil", out var foilProp) && foilProp.ValueKind == JsonValueKind.String)
                    card.UsdFoil = decimal.Parse(foilProp.GetString() ?? "0");

                if (prices.TryGetProperty("eur", out var eurProp) && eurProp.ValueKind == JsonValueKind.String)
                    card.Eur = decimal.Parse(eurProp.GetString() ?? "0");

                if (prices.TryGetProperty("tix", out var tixProp) && tixProp.ValueKind == JsonValueKind.String)
                    card.Tix = decimal.Parse(tixProp.GetString() ?? "0");
            }

            return card;
        }
    }
}
