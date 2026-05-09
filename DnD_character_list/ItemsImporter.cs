using DnD_character_list;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DnD_character_list
{
    internal class ItemsImporter
    {
        public async Task ImportItemsAsync(
            IProgress<(int current, int total, string name)>? progress = null)
        {
            var proxies = new List<string>
            {
                // Add proxy strings here in format "ip:port@user:pass"
            };

            var itemsToAdd = new ConcurrentBag<Item>();

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    Timeout  = 60000
                });

                // ── Collect all URLs first ────────────────────────────────────────
                var page = await browser.NewPageAsync();
                var regularUrls = await GetItemUrls(page, "https://5e14.ttg.club/items", "api/v1/items");
                Console.WriteLine($"[Предметы] Обычных URL: {regularUrls.Count}");

                var pageMagic = await browser.NewPageAsync();
                var magicUrls = await GetItemUrls(pageMagic, "https://5e14.ttg.club/items/magic", "api/v1/items/magic");
                Console.WriteLine($"[Предметы] Магических URL: {magicUrls.Count}");

                await page.CloseAsync();
                await pageMagic.CloseAsync();

                // ── Process with shared counter ───────────────────────────────────
                int   total   = regularUrls.Count + magicUrls.Count;
                int[] counter = { 0 };

                await ProcessItemsSequentially(regularUrls, browser, itemsToAdd,
                    isMagic: false, total, counter, progress);
                await ProcessItemsSequentially(magicUrls,   browser, itemsToAdd,
                    isMagic: true,  total, counter, progress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }

        private async Task<List<string>> GetItemUrls(IPage page, string pageUrl, string apiPattern)
        {
            var urls = new List<string>();
            int scrollAttempts = apiPattern.Contains("/magic") ? 7 : 2;

            await page.GotoAsync(pageUrl);

            for (int i = 0; i <= scrollAttempts; i++)
            {
                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains(apiPattern), new() { Timeout = 60000 });

                var jsonText = await response.TextAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var document = JsonSerializer.Deserialize<JsonElement>(jsonText);
                if (document.ValueKind == JsonValueKind.Array)
                    ExtractUrlsFromDocument(document, urls);

                if (i < scrollAttempts)
                {
                    await page.Mouse.ClickAsync(100, 100);
                    page.Mouse.WheelAsync(0, 10000);   // intentionally not awaited
                }
            }

            return urls.Distinct().ToList();
        }

        private void ExtractUrlsFromDocument(JsonElement document, List<string> urls)
        {
            foreach (var item in document.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && IsBasicSource(item))
                {
                    if (item.TryGetProperty("url", out var urlElement))
                    {
                        var url = urlElement.GetString();
                        if (!string.IsNullOrEmpty(url))
                            urls.Add(url);
                    }
                }
            }
        }

        private async Task ProcessItemsSequentially(
            List<string> urls,
            IBrowser browser,
            ConcurrentBag<Item> bag,
            bool isMagic,
            int total,
            int[] counter,
            IProgress<(int current, int total, string name)>? progress)
        {
            var page       = await browser.NewPageAsync();
            var savedNames = new HashSet<string>();

            foreach (var url in urls)
            {
                string? processedName = null;
                try
                {
                    await Task.Delay(Random.Shared.Next(13000, 15000));

                    var fullUrl = "https://5e14.ttg.club" + url;
                    await page.GotoAsync(fullUrl);

                    var apiUrl   = "/api/v1" + url;
                    var response = await page.WaitForResponseAsync(r =>
                        r.Url.Contains(apiUrl), new() { Timeout = 30000 });

                    var bytes    = await response.BodyAsync();
                    var jsonText = Encoding.UTF8.GetString(bytes);
                    var doc      = JsonSerializer.Deserialize<JsonElement>(jsonText);

                    if (doc.ValueKind == JsonValueKind.Object)
                    {
                        var item = ParseItemFromDocument(doc, isMagic);
                        if (item != null)
                        {
                            processedName = item.Name;
                            bag.Add(item);

                            if (!savedNames.Contains(item.Name))
                            {
                                savedNames.Add(item.Name);
                                try
                                {
                                    using var db = new DDInformationContext();
                                    var existing = db.Items.FirstOrDefault(i => i.Name == item.Name);
                                    if (existing != null)
                                    {
                                        existing.Description = item.Description;
                                        existing.Source      = item.Source;
                                        existing.Price       = item.Price;
                                        existing.Weight      = item.Weight;
                                        existing.IsMagic     = item.IsMagic;
                                        existing.Rarity      = item.Rarity;
                                        existing.ItemType    = item.ItemType;
                                        Console.WriteLine($"[ОБНОВЛЕНО] Предмет: {item.Name}");
                                    }
                                    else
                                    {
                                        db.Items.Add(item);
                                        Console.WriteLine($"[ДОБАВЛЕНО] Предмет: {item.Name}");
                                    }
                                    await db.SaveChangesAsync();
                                }
                                catch (Exception saveEx)
                                {
                                    Console.WriteLine($"[ОШИБКА] {item.Name}: {saveEx.InnerException?.Message ?? saveEx.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка URL {url}: {ex.Message}");
                }
                finally
                {
                    var cur = Interlocked.Increment(ref counter[0]);
                    progress?.Report((cur, total, processedName ?? url));
                }
            }

            await page.CloseAsync();
        }

        private Item? ParseItemFromDocument(JsonElement doc, bool isMagic)
        {
            string name     = "";
            string desc     = "";
            string source   = "";
            string price    = "";
            float? weight   = null;
            string rarity   = isMagic ? "" : "не магические";
            string itemType = "";

            foreach (var prop in doc.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "name" when prop.Value.ValueKind == JsonValueKind.Object:
                        if (prop.Value.TryGetProperty("rus", out var rusName))
                            name = rusName.GetString() ?? "";
                        break;

                    case "description":
                        desc = StripHtml(prop.Value.GetString() ?? "");
                        break;

                    case "source" when prop.Value.ValueKind == JsonValueKind.Object:
                        if (prop.Value.TryGetProperty("shortName", out var sn))
                            source = sn.GetString() ?? "";
                        break;

                    case "price" when prop.Value.ValueKind == JsonValueKind.Object:
                        {
                            string count = "";
                            string unit  = "";
                            if (prop.Value.TryGetProperty("dmg", out var countEl)) count = countEl.ToString();
                            if (prop.Value.TryGetProperty("xge",  out var unitEl))  unit  = unitEl.GetString() ?? unitEl.ToString();
                            price = $"{count} {unit}".Trim();
                        }
                        break;

                    case "price" when prop.Value.ValueKind == JsonValueKind.String:
                        price = prop.Value.GetString() ?? "";
                        break;

                    case "weight":
                        if (prop.Value.ValueKind == JsonValueKind.Number)
                            weight = prop.Value.GetSingle();
                        else if (float.TryParse(prop.Value.ToString(), out float w))
                            weight = w;
                        break;

                    case "rarity":
                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            if (prop.Value.TryGetProperty("name", out var rarRus))
                                rarity = rarRus.GetString() ?? rarity;
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.String)
                            rarity = prop.Value.GetString() ?? rarity;
                        break;

                    case "type" when prop.Value.ValueKind == JsonValueKind.Object:
                        if (prop.Value.TryGetProperty("name", out var typeRus))
                            itemType = typeRus.GetString() ?? "";
                        break;

                    case "type" when prop.Value.ValueKind == JsonValueKind.String:
                        itemType = prop.Value.GetString() ?? "";
                        break;
                }
            }

            if (string.IsNullOrEmpty(name)) return null;

            return new Item
            {
                Name        = name,
                Description = desc,
                Source      = source,
                Price       = price,
                Weight      = weight,
                IsMagic     = isMagic,
                Rarity      = string.IsNullOrEmpty(rarity)
                                  ? (isMagic ? "редкость не определена" : "не магические")
                                  : rarity,
                ItemType    = itemType
            };
        }

        private bool IsBasicSource(JsonElement item)
        {
            if (item.TryGetProperty("source", out var sourceElement) &&
                sourceElement.ValueKind == JsonValueKind.Object)
            {
                if (sourceElement.TryGetProperty("group", out var groupElement) &&
                    groupElement.ValueKind == JsonValueKind.Object &&
                    groupElement.TryGetProperty("shortName", out var shortNameElement))
                {
                    return shortNameElement.GetString() == "Basic";
                }
            }
            return false;
        }

        private static string StripHtml(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]+>", "");
    }
}
