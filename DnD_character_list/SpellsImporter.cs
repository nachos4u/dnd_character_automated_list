using DnD_character_list;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Microsoft.VisualBasic.Devices;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DnD_character_list
{
    internal class SpellsImporter
    {
        public int count_spell = 0;

        public async Task ImportSpellAsync()
        {
            var proxies = new List<string>
    {
                "188.130.184.6:3000@YELcgn68:PYFuji2l"

            };
            var spellToAdd = new ConcurrentBag<Spell>();
            var browserTasks = new List<Task>();
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
                await page.GotoAsync("https://5e14.ttg.club/spells");

                // Get all spell URLs
                var urls = await GetAllSpellUrls(page);

                // Process spells in batches
                await ProcessSpellsInBatches(urls, proxies, spellToAdd, browserTasks);


                // Save to database
                await SaveSpellsToDatabase(spellToAdd);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }

                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private async Task<List<string>> GetAllSpellUrls(IPage page)
        {
            var urls = new List<string>();
            var scrollAttempts = 6;

            for (int i = 0; i <= scrollAttempts; i++)
            {
                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains("api/v1/spells"), new() { Timeout = 60000 });

                var jsonText = await response.TextAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var document = JsonSerializer.Deserialize<JsonElement>(jsonText);

                if (document.ValueKind == JsonValueKind.Array)
                {
                    ExtractUrlsFromDocument(document, urls);
                }

                if (i < scrollAttempts) // Don't scroll after last iteration
                {
                    await page.Mouse.ClickAsync(100, 100);
                    page.Mouse.WheelAsync(0, 10000);
                }
            }

            return urls;
        }

        private void ExtractUrlsFromDocument(JsonElement document, List<string> urls)
        {
            foreach (var items in document.EnumerateArray())
            {
                if (items.ValueKind == JsonValueKind.Object && IsBasicSource(items))
                {
                    if (items.TryGetProperty("url", out var urlElement))
                    {
                        var url = urlElement.GetString();
                        if (!string.IsNullOrEmpty(url))
                            urls.Add(url);
                    }
                }
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

        private async Task ProcessSpellsInBatches(List<string> urls, List<string> proxies, ConcurrentBag<Spell> spellToAdd, List<Task> browserTasks)
        {


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
                    Server = $"https://{ipPort}", // или socks5://
                    Username = creds[0],
                    Password = creds[1]
                };

                browserTasks.Add(Task.Run(async () =>
                {
                    using var playwrightLocal = await Playwright.CreateAsync();
                    await using var browserLocal = await playwrightLocal.Chromium.LaunchAsync(new()
                    {
                        Headless = false,
                        Proxy = proxy
                    });

                    var pageLocal = await browserLocal.NewPageAsync();

                    HashSet<string> savedNames;
                    using (var dbCheck = new DDInformationContext())
                        savedNames = dbCheck.Spells.Select(s => s.Name).ToHashSet();

                    foreach (var url in urlsChunk)
                    {
                        try
                        {
                            await Task.Delay(Random.Shared.Next(13000, 15000)); // Reduced delay

                            var fullUrl = "https://5e14.ttg.club" + url;
                            await pageLocal.GotoAsync(fullUrl);

                            var apiUrl = "/api/v1" + url;
                            var response = await pageLocal.WaitForResponseAsync(r =>
                                r.Url.Contains(apiUrl), new() { Timeout = 30000 });

                            var bytes = await response.BodyAsync();
                            string jsonText = Encoding.UTF8.GetString(bytes);

                            var document = JsonSerializer.Deserialize<JsonElement>(jsonText);

                            if (document.ValueKind == JsonValueKind.Object)
                            {
                                var spell = ParseSpellFromDocument(document);
                                if (spell != null)
                                {
                                    spellToAdd.Add(spell);
                                    if (!savedNames.Contains(spell.Name))
                                    {
                                        savedNames.Add(spell.Name);
                                        try
                                        {
                                            using var dbSave = new DDInformationContext();
                                            dbSave.Spells.Add(spell);
                                            await dbSave.SaveChangesAsync();
                                            Console.WriteLine($"[УСПЕХ] Добавлено заклинание: {spell.Name}");
                                        }
                                        catch (Exception saveEx)
                                        {
                                            Console.WriteLine($"[ОШИБКА сохранения] {spell.Name}: {saveEx.InnerException?.Message ?? saveEx.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка с прокси {proxy} для URL {url}: {ex.Message}");
                        }
                    }
                }));
            }

            await Task.WhenAll(browserTasks);
        }

        private Spell ParseSpellFromDocument(JsonElement property)
        {
            string name = "";
            string peculiarities = "";
            int cell_level = 0;
            string source = "";
            string school = "";
            string desc = "";
            string components = "";
            string range = "";
            string duration = "";
            string time = "";
            string upper = "";
            string material_component = "";

            foreach (var raceDocument in property.EnumerateObject())
            {
                switch (raceDocument.Name)
                {
                    case "name" when raceDocument.Value.ValueKind == JsonValueKind.Object:
                        name = raceDocument.Value.GetProperty("rus").GetString() ?? "";
                        break;
                    case "level":
                        if (int.TryParse(raceDocument.Value.ToString(), out int level))
                            cell_level = level;
                        break;
                    case "school":
                        school = raceDocument.Value.ToString();
                        break;
                    case "components" when raceDocument.Value.ValueKind == JsonValueKind.Object:
                        components = ParseComponents(raceDocument.Value);
                        break;
                    case "source" when raceDocument.Value.ValueKind == JsonValueKind.Object:
                        source = raceDocument.Value.GetProperty("shortName").GetString() ?? "";
                        break;
                    case "range":
                        range = raceDocument.Value.ToString();
                        break;
                    case "duration":
                        duration = raceDocument.Value.ToString();
                        break;
                    case "time":
                        time = raceDocument.Value.ToString();
                        break;
                    case "classes" when raceDocument.Value.ValueKind == JsonValueKind.Array:
                        peculiarities = ParseClasses(raceDocument.Value);
                        break;
                    case "description":
                        desc = StripHtml(raceDocument.Value.GetString() ?? "");
                        break;
                    case "upper":
                        upper = StripHtml(raceDocument.Value.ToString());
                        break;
                }
            }


            if (!string.IsNullOrEmpty(name))
            {
                return new Spell
                {
                    Name = name,
                    Description = desc,
                    CellLevel = cell_level,
                    Source = source,
                    Range = range,
                    School = school,
                    Components = components,
                    Duration = duration,
                    Time = time,
                    Peculiarities = peculiarities,
                    MaterialComponent = material_component,
                    Upper = upper
                };
            }

            return null;
        }

        private string ParseComponents(JsonElement componentsElement)
        {
            var components = new StringBuilder();
            foreach (var component in componentsElement.EnumerateObject())
            {
                switch (component.Name)
                {
                    case "v": components.Append("v;"); break;
                    case "s": components.Append("s;"); break;
                    case "m": components.Append("m;"); break;
                }
            }
            return components.ToString();
        }

        private string ParseClasses(JsonElement classesElement)
        {
            var classes = new StringBuilder();
            foreach (var classItem in classesElement.EnumerateArray())
            {
                foreach (var classProperty in classItem.EnumerateObject())
                {
                    if (classProperty.Name == "name")
                    {
                        classes.Append(classProperty.Value.ToString());
                    }
                }
            }
            return classes.ToString();
        }

        // Fix 7: убираем HTML-разметку, оставляем текст
        private static string StripHtml(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]+>", "");

        private async Task SaveSpellsToDatabase(ConcurrentBag<Spell> spellToAdd)
        {
            using var db = new DDInformationContext();
            {
                var newSpells = new List<Spell>();
                lock (spellToAdd)
                {
                    var existingSpellNames = db.Spells
                        .Select(s => s.Name)
                        .ToHashSet();

                    newSpells = spellToAdd
                        .Where(sp => !existingSpellNames.Contains(sp.Name))
                        .GroupBy(sp => sp.Name)
                        .Select(g => g.First())
                        .ToList();
                }

                if (newSpells.Any())
                {
                    await db.Spells.AddRangeAsync(newSpells);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"Добавлено {newSpells.Count} новых заклинаний");
                }
            }
        }
    }
}
