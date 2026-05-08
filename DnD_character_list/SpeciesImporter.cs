using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DnD_character_list
{
    internal class SpeciesImporter
    {
        public async Task ImportRacesAsync()
        {
            var proxies = new List<string>
            {
                "188.130.184.6:3000@YELcgn68:PYFuji2l"
            };
            var speciesToAdd = new ConcurrentBag<Species>();
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
                await page.GotoAsync("https://5e14.ttg.club/races");

                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains("/api/v1/races"), new() { Timeout = 60000 });

                var jsonText = await response.TextAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var document = JsonSerializer.Deserialize<JsonElement>(jsonText);
                if (document.ValueKind != JsonValueKind.Array) return;

                var urls = new List<string>();
                foreach (var item in document.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;

                    bool isBasicSource = false;
                    bool isMPMM = false;

                    if (item.TryGetProperty("source", out var sourceElement) &&
                        sourceElement.ValueKind == JsonValueKind.Object)
                    {
                        if (sourceElement.TryGetProperty("group", out var groupElement) &&
                            groupElement.ValueKind == JsonValueKind.Object &&
                            groupElement.TryGetProperty("shortName", out var shortName))
                            isBasicSource = shortName.GetString() == "Basic";

                        if (sourceElement.TryGetProperty("shortName", out var shortNameDirect))
                            isMPMM = shortNameDirect.GetString() == "MPMM";
                    }

                    if (!isBasicSource || isMPMM)
                    {
                        string raceName = item.GetProperty("name").GetProperty("rus").GetString() ?? "Без имени";
                        Console.WriteLine($"Пропущена раса: {raceName} (Basic: {isBasicSource}, MPMM: {isMPMM})");
                        continue;
                    }

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

                        // Tracks names processed in this run only — no DB pre-load,
                        // so existing records are always updated.
                        var savedNames = new HashSet<string>();

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
                                string jsonTextRace = Encoding.UTF8.GetString(bytes);

                                var raceDocument = JsonSerializer.Deserialize<JsonElement>(jsonTextRace);

                                if (raceDocument.ValueKind == JsonValueKind.Object)
                                {
                                    var parsed = ParseMainRace(raceDocument);

                                    foreach (var species in parsed)
                                    {
                                        speciesToAdd.Add(species);

                                        // Skip if already processed in this run (duplicate URL / subrace overlap)
                                        if (savedNames.Contains(species.Name)) continue;
                                        savedNames.Add(species.Name);

                                        try
                                        {
                                            using var dbSave = new DDInformationContext();
                                            var existing = dbSave.Species.FirstOrDefault(s => s.Name == species.Name);
                                            if (existing != null)
                                            {
                                                existing.SpeciesChaTics = species.SpeciesChaTics;
                                                existing.SpeciesSkills  = species.SpeciesSkills;
                                                existing.Description    = species.Description;
                                                existing.Source         = species.Source;
                                                existing.Speed          = species.Speed;
                                                Console.WriteLine($"[ОБНОВЛЕНО] Вид: {species.Name}");
                                            }
                                            else
                                            {
                                                dbSave.Species.Add(species);
                                                Console.WriteLine($"[ДОБАВЛЕНО] Вид: {species.Name}");
                                            }
                                            await dbSave.SaveChangesAsync();
                                        }
                                        catch (Exception saveEx)
                                        {
                                            Console.WriteLine($"[ОШИБКА сохранения] {species.Name}: {saveEx.InnerException?.Message ?? saveEx.Message}");
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
                await SaveSpeciesToDatabase(speciesToAdd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }

        private List<Species> ParseMainRace(JsonElement property)
        {
            var result = new List<Species>();
            string name = "";
            string skill = "";
            string char_tics = "";
            string source = "";
            string speed = "";
            string desc = "";

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
                    case "description":
                        desc = StripHtml(prop.Value.GetString() ?? "");
                        break;
                    case "abilities" when prop.Value.ValueKind == JsonValueKind.Array:
                        char_tics = ParseAbilities(prop.Value);
                        break;
                    case "speed" when prop.Value.ValueKind == JsonValueKind.Array:
                        speed = ParseSpeed(prop.Value);
                        break;
                    case "skills" when prop.Value.ValueKind == JsonValueKind.Array:
                        skill = ParseSkills(prop.Value);
                        break;
                    case "subraces":
                        result.AddRange(ParseSubraces(prop));
                        break;
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                result.Insert(0, new Species
                {
                    Name = name,
                    SpeciesChaTics = char_tics,
                    SpeciesSkills = skill,
                    Description = desc,
                    Source = source,
                    Speed = speed
                });
            }

            return result;
        }

        private List<Species> ParseSubraces(JsonProperty property)
        {
            var result = new List<Species>();
            if (property.Value.ValueKind != JsonValueKind.Array) return result;

            foreach (var subrace in property.Value.EnumerateArray())
            {
                if (subrace.ValueKind != JsonValueKind.Object) continue;

                string name = "";
                string skill = "";
                string char_tics = "";
                string source = "";
                string speed = "";
                string desc = "";

                foreach (var prop in subrace.EnumerateObject())
                {
                    switch (prop.Name)
                    {
                        case "name" when prop.Value.ValueKind == JsonValueKind.Object:
                            name = prop.Value.GetProperty("rus").GetString() ?? "";
                            break;
                        case "source" when prop.Value.ValueKind == JsonValueKind.Object:
                            source = prop.Value.GetProperty("shortName").GetString() ?? "";
                            break;
                        case "description":
                            desc = StripHtml(prop.Value.GetString() ?? "");
                            break;
                        case "abilities" when prop.Value.ValueKind == JsonValueKind.Array:
                            char_tics = ParseAbilities(prop.Value);
                            break;
                        case "speed" when prop.Value.ValueKind == JsonValueKind.Array:
                            speed = ParseSpeed(prop.Value);
                            break;
                        case "skills" when prop.Value.ValueKind == JsonValueKind.Array:
                            skill = ParseSkills(prop.Value);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(name))
                {
                    result.Add(new Species
                    {
                        Name = name,
                        SpeciesChaTics = char_tics,
                        SpeciesSkills = skill,
                        Description = desc,
                        Source = source,
                        Speed = speed
                    });
                }
            }

            return result;
        }

        private string ParseAbilities(JsonElement abilitiesElement)
        {
            var result = "";
            foreach (var ability in abilitiesElement.EnumerateArray())
            {
                var names = new List<string>();
                var values = new List<string>();

                foreach (var prop in ability.EnumerateObject())
                {
                    if (prop.Name == "name")
                    {
                        switch (prop.Value.GetString())
                        {
                            case "Сила": names.Add("сил"); break;
                            case "Ловкость": names.Add("лов"); break;
                            case "Телосложение": names.Add("тел"); break;
                            case "Интеллект": names.Add("инт"); break;
                            case "Мудрость": names.Add("муд"); break;
                            case "Харизма": names.Add("хар"); break;
                            case "+2 и +1 / +1 к трем": names.Add("21/111"); break;
                            case "к другой": names.Add("другой"); break;
                            case "к 2 другим": names.Add("2другим"); break;
                        }
                    }
                    else if (prop.Name == "value")
                    {
                        values.Add(prop.Value.ToString());
                    }
                }

                for (int k = 0; k < names.Count; k++)
                    result += names[k] + ":" + values[k] + ";";
            }
            return result;
        }

        private string ParseSpeed(JsonElement speedElement)
        {
            var result = "";
            var types = new List<string>();
            var values = new List<string>();

            foreach (var speedEntry in speedElement.EnumerateArray())
            {
                foreach (var prop in speedEntry.EnumerateObject())
                {
                    if (prop.Name == "name")
                    {
                        switch (prop.Value.GetString())
                        {
                            case "летая": types.Add("лет"); break;
                            case "плавая": types.Add("пла"); break;
                        }
                    }
                    else if (prop.Name == "value")
                    {
                        values.Add(prop.Value.ToString());
                    }
                }

                for (int k = 0; k < types.Count; k++)
                    result += types[k] + ":" + values[k + 1] + ";";
            }

            if (values.Count > 0)
                result += "пеш:" + values[0] + ";";

            return result;
        }

        private string ParseSkills(JsonElement skillsElement)
        {
            var result = "";
            foreach (var skill in skillsElement.EnumerateArray())
            {
                foreach (var prop in skill.EnumerateObject())
                {
                    if (prop.Name == "name")
                        result += prop.Value.GetString() + ": ";
                    else if (prop.Name == "description")
                        result += StripHtml(prop.Value.GetString() ?? "") + "\n\n";
                }
            }
            return result;
        }

        private static string StripHtml(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]+>", "");

        private async Task SaveSpeciesToDatabase(ConcurrentBag<Species> speciesToAdd)
        {
            using var db = new DDInformationContext();

            // Deduplicate by name within the collected bag
            var unique = speciesToAdd
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .ToList();

            int added = 0, updated = 0;
            foreach (var species in unique)
            {
                var existing = db.Species.FirstOrDefault(s => s.Name == species.Name);
                if (existing != null)
                {
                    existing.SpeciesChaTics = species.SpeciesChaTics;
                    existing.SpeciesSkills  = species.SpeciesSkills;
                    existing.Description    = species.Description;
                    existing.Source         = species.Source;
                    existing.Speed          = species.Speed;
                    updated++;
                }
                else
                {
                    db.Species.Add(species);
                    added++;
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"Итог: добавлено {added}, обновлено {updated} видов");
        }
    }
}
