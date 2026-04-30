using Microsoft.Playwright;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnD_character_list
{
    internal class BackgroundImporter
    {
        public int count_background = 0;

        public async Task ImportBackgroundAsync()
        {
            Random random = new Random();
            var proxies = new List<string>
            {
                    "http://45.153.231.229:8080"
            };
            var browserTasks = new List<Task>();
            var backgroundToAdd = new ConcurrentBag<Background>();
            int count = 0;

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false,  // Changed to true for production
                    Timeout = 60000
                });

                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://5e14.ttg.club/backgrounds");

                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains("api/v1/backgrounds"), new() { Timeout = 60000 });

                var jsonText = await response.TextAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var document = JsonSerializer.Deserialize<JsonElement>(jsonText);

                if (document.ValueKind == JsonValueKind.Array)
                {
                    var urls = new List<string>();

                    foreach (var items in document.EnumerateArray())
                    {
                        if (items.ValueKind == JsonValueKind.Object)
                        {
                            bool isBasicSource = false;

                            if (items.TryGetProperty("source", out var sourceElement) &&
                                sourceElement.ValueKind == JsonValueKind.Object)
                            {
                                // Проверяем group.shortName == "Basic"
                                if (sourceElement.TryGetProperty("group", out var groupElement) &&
                                    groupElement.ValueKind == JsonValueKind.Object &&
                                    groupElement.TryGetProperty("shortName", out var shortNameElement))
                                {
                                    isBasicSource = shortNameElement.GetString() == "Basic";
                                }

                            }

                            // Пропускаем, если не Basic ИЛИ если это MPMM
                            if (!isBasicSource)
                            {
                                continue;
                            }

                            if (items.TryGetProperty("url", out var urlElement))
                            {
                                var url = urlElement.GetString();
                                if (!string.IsNullOrEmpty(url))
                                    urls.Add(url);
                            }
                        }
                    }



                    var chunks = urls
                        .Select((url, i) => new { url, i })
                        .GroupBy(x => x.i % proxies.Count)
                        .Select(g => g.Select(x => x.url).ToList())
                        .ToList();

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        var proxy = proxies[i];
                        var urlsChunk = chunks[i];

                        browserTasks.Add(Task.Run(async () =>
                        {
                            using var playwrightLocal = await Playwright.CreateAsync();
                            await using var browserLocal = await playwrightLocal.Chromium.LaunchAsync(new()
                            {
                                Headless = false,
                                Proxy = new Proxy
                                {
                                    Server = proxy
                                }
                            });

                            var pageLocal = await browserLocal.NewPageAsync();

                            foreach (var url in urlsChunk)
                            {
                                using (var dbLocal = new DDInformationContext())
                                {
                                    try
                                    {
                                        await Task.Delay(Random.Shared.Next(13000, 15000));

                                        var fullUrl = "https://5e14.ttg.club" + url;
                                        await pageLocal.GotoAsync(fullUrl);


                                        // Fix: Use the correct URL for API
                                        var apiUrl = "/api/v1" + url;
                                        var response_race = await pageLocal.WaitForResponseAsync(r =>
                                            r.Url.Contains(apiUrl));

                                        // Fix: Use response_race instead of response
                                        var bytes = await response_race.BodyAsync();
                                        string jsonTextRace = Encoding.UTF8.GetString(bytes); // Changed to UTF-8

                                        var raceDocument = JsonSerializer.Deserialize<JsonElement>(jsonTextRace);

                                        if (raceDocument.ValueKind == JsonValueKind.Object)
                                        {

                                            await ProcessBackground(raceDocument, backgroundToAdd, count);


                                        }


                                        // Reduced delay
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Ошибка с прокси {proxy} для URL {url}: {ex.Message}");
                                    }
                                }
                            }
                        }));
                    }
                    await Task.WhenAll(browserTasks);

                    using (var db = new DDInformationContext())
                    {
                        foreach (var bg in backgroundToAdd)
                        {
                            if (!db.Backgrounds.Any(b => b.Name == bg.Name))
                            {
                                db.Backgrounds.Add(bg);
                            }
                        }
                        await db.SaveChangesAsync();
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка:\n" + ex.Message);
            }
        }


        private async Task ProcessBackground(JsonElement property, ConcurrentBag<Background> backgroundToAdd, int count_background)
        {
            string name = "";
            string skill = "";
            int gm = 0;
            string source = "";
            string invetory = "";
            string desc = "";
            string tools = "";
            foreach (var raceDocument in property.EnumerateObject())
            {
                if (raceDocument.Name == "name" && raceDocument.Value.ValueKind == JsonValueKind.Object)
                {
                    name = raceDocument.Value.GetProperty("rus").GetString() ?? "";
                }

                else if (raceDocument.Name == "source" && raceDocument.Value.ValueKind == JsonValueKind.Object)
                {
                    source = raceDocument.Value.GetProperty("shortName").GetString() ?? "";
                }



                else if (raceDocument.Name == "skills" && raceDocument.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var skillItem in raceDocument.Value.EnumerateArray())
                    {
                        switch (skillItem.ToString())
                        {
                            case "Атлетика":
                                skill += "атлетика;";
                                break;
                            case "Акробатика":
                                skill += "акробатика;";
                                break;
                            case "Ловкость рук":
                                skill += "ловкость рук;";
                                break;
                            case "Скрытность":
                                skill += "скрытность;";
                                break;
                            case "Анализ":
                                skill += "анализ;";
                                break;
                            case "История":
                                skill += "история;";
                                break;
                            case "Магия":
                                skill += "магия;";
                                break;
                            case "Природа":
                                skill += "природа;";
                                break;
                            case "Религия":
                                skill += "религия;";
                                break;
                            case "Восприятие":
                                skill += "восприятие;";
                                break;
                            case "Выживание":
                                skill += "выживание;";
                                break;
                            case "Медицина":
                                skill += "медицина;";
                                break;
                            case "Проницание":
                                skill += "проницание;";
                                break;
                            case "Выступление":
                                skill += "выступление;";
                                break;
                            case "Запугивание":
                                skill += "запугивание;";
                                break;
                            case "Обман":
                                skill += "обман;";
                                break;
                            case "Убеждение":
                                skill += "убеждение;";
                                break;
                        }
                    }
                }
                else if (raceDocument.Name == "toolOwnership")
                {
                    tools = raceDocument.Value.GetString() ?? "";
                }
                else if (raceDocument.Name == "equipments")
                {
                    foreach (var item in raceDocument.Value.EnumerateArray())
                    {
                        invetory += item.ToString() + "; \n";
                    }
                }
                else if (raceDocument.Name == "startGold")
                {
                    gm = int.Parse(raceDocument.Value.ToString());
                }
                else if (raceDocument.Name == "description")
                {
                    desc = raceDocument.Value.GetString() ?? "";
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                var newBackground = new Background
                {
                    Name = name,
                    Description = desc,
                    Source = source,
                    Possesion = skill,
                    Gm = gm,
                    Invetary = invetory,
                    ToolOwnership = tools
                };

                // Thread-safe добавление
                if (!backgroundToAdd.Any(b => b.Name == name))
                {
                    backgroundToAdd.Add(newBackground);
                    Interlocked.Increment(ref count_background);
                    Console.WriteLine($"[УСПЕХ] Добавлена основная раса: {name}");
                }
            }
        }
    }
}
