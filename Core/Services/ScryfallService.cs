using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
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

        static ScryfallService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MyTelegramBot/1.0");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public async Task<ScryfallCard?> GetCardPriceAsync(
            string name,
            string? set = null,
            string? collectorNumber = null,
            CancellationToken ct = default)
        {
            string url;

            if (!string.IsNullOrEmpty(set) && !string.IsNullOrEmpty(collectorNumber))
            {
                url = $"cards/search?q=set:{set}+number:{collectorNumber}";
            }
            else
            {
                url = $"cards/named?exact={Uri.EscapeDataString(name)}";
            }

            Console.WriteLine($"[ScryfallService] URL: {url}");

            var response = await _http.GetAsync(url, ct);
            Console.WriteLine($"[ScryfallService] Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"[ScryfallService] Error: {response.ReasonPhrase} - {errorContent}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
            {
                root = dataArray[0];
            }

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
                    card.Usd = decimal.Parse(usdProp.GetString() ?? "0", CultureInfo.InvariantCulture);

                if (prices.TryGetProperty("usd_foil", out var foilProp) && foilProp.ValueKind == JsonValueKind.String)
                    card.UsdFoil = decimal.Parse(foilProp.GetString() ?? "0", CultureInfo.InvariantCulture);

                if (prices.TryGetProperty("eur", out var eurProp) && eurProp.ValueKind == JsonValueKind.String)
                    card.Eur = decimal.Parse(eurProp.GetString() ?? "0", CultureInfo.InvariantCulture);

                if (prices.TryGetProperty("tix", out var tixProp) && tixProp.ValueKind == JsonValueKind.String)
                    card.Tix = decimal.Parse(tixProp.GetString() ?? "0", CultureInfo.InvariantCulture);
            }
            return card;
        }
    }
}