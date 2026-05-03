using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DnD_character_list
{
    internal class BackgroundImporter
    {
        public async Task ImportBackgroundAsync()
        {
            var proxies = new List<string>
            {
                "188.130.184.6:3000@YELcgn68:PYFuji2l"
            };
            var backgroundToAdd = new ConcurrentBag<Background>();
            var browserTasks = new List<Task>();

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false,
                    Timeout = 60000
                });

                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://5e14.ttg.club/backgrounds");

                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains("api/v1/backgrounds"), new() { Timeout = 60000 });

                var jsonText = await response.TextAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var document = JsonSerializer.Deserialize<JsonElement>(jsonText);

                if (document.ValueKind != JsonValueKind.Array) return;

                var urls = new List<string>();
                foreach (var item in document.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    if (!IsBasicSource(item)) continue;

                    if (item.TryGetProperty("url", out var urlElement))
                    {
                        var url = urlElement.GetString();
                        if (!string.IsNullOrEmpty(url))
                            urls.Add(url);
                    }
                }

                var chunks = urls
                    .Select((url, i) => new { url, i })
                    .GroupBy(x => x.i % proxies.Count)
                    .Select(g => g.Select(x => x.url).ToList())
                    .ToList();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var urlsChunk = chunks[i];
                    var parts = proxies[i].Split('@');
                    var ipPort = parts[0];
                    var creds = parts[1].Split(':');

                    var proxy = new Proxy
                    {
                        Server = $"https://{ipPort}",
                        Username = creds[0],
                        Password = creds[1]
                    };

                    browserTasks.Add(Task.Run(async () =>
                    {
                        using var playwrightLocal = await Playwright.CreateAsync();
                        await using var browserLocal = await playwrightLocal.Chromium.LaunchAsync(new()
                        {
                            Headless = false
                        });

                        var pageLocal = await browserLocal.NewPageAsync();

                        HashSet<string> savedNames;
                        using (var dbCheck = new DDInformationContext())
                            savedNames = dbCheck.Backgrounds.Select(b => b.Name).ToHashSet();

                        foreach (var url in urlsChunk)
                        {
                            try
                            {
                                await Task.Delay(Random.Shared.Next(13000, 15000));

                                var fullUrl = "https://5e14.ttg.club" + url;
                                await pageLocal.GotoAsync(fullUrl);

                                var apiUrl = "/api/v1" + url;
                                var apiResponse = await pageLocal.WaitForResponseAsync(r =>
                                    r.Url.Contains(apiUrl), new() { Timeout = 30000 });

                                var bytes = await apiResponse.BodyAsync();
                                string jsonTextBg = Encoding.UTF8.GetString(bytes);

                                var bgDocument = JsonSerializer.Deserialize<JsonElement>(jsonTextBg);

                                if (bgDocument.ValueKind == JsonValueKind.Object)
                                {
                                    var bg = ParseBackground(bgDocument);
                                    if (bg != null)
                                    {
                                        backgroundToAdd.Add(bg);
                                        if (!savedNames.Contains(bg.Name))
                                        {
                                            savedNames.Add(bg.Name);
                                            try
                                            {
                                                using var dbSave = new DDInformationContext();
                                                dbSave.Backgrounds.Add(bg);
                                                await dbSave.SaveChangesAsync();
                                                Console.WriteLine($"[УСПЕХ] Добавлен фон: {bg.Name}");
                                            }
                                            catch (Exception saveEx)
                                            {
                                                Console.WriteLine($"[ОШИБКА сохранения] {bg.Name}: {saveEx.InnerException?.Message ?? saveEx.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка для URL {url}: {ex.Message}");
                            }
                        }
                    }));
                }

                await Task.WhenAll(browserTasks);
                await SaveBackgroundsToDatabase(backgroundToAdd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
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

        private Background ParseBackground(JsonElement property)
        {
            string name = "";
            string skill = "";
            int gm = 0;
            string source = "";
            string inventory = "";
            string desc = "";
            string tools = "";

            foreach (var prop in property.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "name" when prop.Value.ValueKind == JsonValueKind.Object:
                        name = prop.Value.GetProperty("rus").GetString() ?? "";
                        break;
                    case "source" when prop.Value.ValueKind == JsonValueKind.Object:
                        source = prop.Value.GetProperty("shortName").GetString() ?? "";
                        break;
                    case "skills" when prop.Value.ValueKind == JsonValueKind.Array:
                        foreach (var skillItem in prop.Value.EnumerateArray())
                            skill += skillItem.GetString()?.ToLower() + ";";
                        break;
                    case "toolOwnership":
                        tools = prop.Value.GetString() ?? "";
                        break;
                    case "equipments" when prop.Value.ValueKind == JsonValueKind.Array:
                        foreach (var item in prop.Value.EnumerateArray())
                            inventory += item.ToString() + "; \n";
                        break;
                    case "startGold":
                        if (int.TryParse(prop.Value.ToString(), out int gold))
                            gm = gold;
                        break;
                    case "description":
                        desc = prop.Value.GetString() ?? "";
                        break;
                }
            }

            if (string.IsNullOrEmpty(name)) return null;

            return new Background
            {
                Name = name,
                Description = desc,
                Source = source,
                Possesion = skill,
                Gm = gm,
                Invetary = inventory,
                ToolOwnership = tools
            };
        }

        private async Task SaveBackgroundsToDatabase(ConcurrentBag<Background> backgroundToAdd)
        {
            using var db = new DDInformationContext();
            var existingNames = db.Backgrounds.Select(b => b.Name).ToHashSet();

            var newBackgrounds = backgroundToAdd
                .Where(b => !existingNames.Contains(b.Name))
                .GroupBy(b => b.Name)
                .Select(g => g.First())
                .ToList();

            if (newBackgrounds.Any())
            {
                await db.Backgrounds.AddRangeAsync(newBackgrounds);
                await db.SaveChangesAsync();
                Console.WriteLine($"Добавлено {newBackgrounds.Count} новых предысторий");
            }
        }
    }
}
