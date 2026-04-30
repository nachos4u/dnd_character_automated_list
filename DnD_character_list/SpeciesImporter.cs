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
    internal class SpeciesImporter
    {
        public int count_race = 0;

        public async Task ImportRacesAsync()
        {
            Random random = new Random();
            var proxies = new List<string>
            {
                    "http://45.153.231.229:8080"
            };
            var browserTasks = new List<Task>();
            var speciesToAdd = new ConcurrentBag<Species>();
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
                await page.GotoAsync("https://5e14.ttg.club/races");

                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains("/api/v1/races"), new() { Timeout = 60000 });

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
                            bool isMPMM = false;

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

                                // Проверяем сам shortName == "MPMM"
                                if (sourceElement.TryGetProperty("shortName", out var shortNameDirect))
                                {
                                    isMPMM = shortNameDirect.GetString() == "MPMM";
                                }
                            }

                            // Пропускаем, если не Basic ИЛИ если это MPMM
                            if (!isBasicSource || isMPMM)
                            {
                                string raceName = items.GetProperty("name").GetProperty("rus").GetString() ?? "Без имени";
                                Console.WriteLine($"Пропущена раса: {raceName} (Basic: {isBasicSource}, MPMM: {isMPMM})");
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

                                            await ProcessMainRace(raceDocument, speciesToAdd, count);


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
                        foreach (var sp in speciesToAdd)
                        {
                            if (!db.Species.Any(s => s.Name == sp.Name))
                            {
                                db.Species.Add(sp);
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

        private async Task ProcessSubraces(JsonProperty property, ConcurrentBag<Species> speciesToAdd, int count)
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var subrace in property.Value.EnumerateArray())
                {
                    string namesub = "";
                    string skillsub = "";
                    string char_ticssub = "";
                    string sourcesub = "";
                    string speedsub = "";
                    string descsub = "";

                    if (subrace.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var obj3 in subrace.EnumerateObject())
                        {
                            if (obj3.Name == "name" && obj3.Value.ValueKind == JsonValueKind.Object)
                                namesub = obj3.Value.GetProperty("rus").GetString() ?? "";

                            else if (obj3.Name == "abilities" && obj3.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var ability in obj3.Value.EnumerateArray())
                                {
                                    var abb_name = new List<string>();
                                    var abb_value = new List<string>();

                                    foreach (var abilityProp in ability.EnumerateObject())
                                    {
                                        if (abilityProp.Name == "name")
                                        {
                                            switch (abilityProp.Value.GetString())
                                            {
                                                case "Сила": abb_name.Add("сил"); break;
                                                case "Ловкость": abb_name.Add("лов"); break;
                                                case "Телосложение": abb_name.Add("тел"); break;
                                                case "Интеллект": abb_name.Add("инт"); break;
                                                case "Мудрость": abb_name.Add("муд"); break;
                                                case "Харизма": abb_name.Add("хар"); break;
                                                case "+2 и +1 / +1 к трем": abb_name.Add("21/111"); break;
                                                case "к другой": abb_name.Add("другой"); break;
                                                case "к 2 другим": abb_name.Add("2другим"); break;
                                            }
                                        }
                                        else if (abilityProp.Name == "value")
                                        {
                                            abb_value.Add(abilityProp.Value.ToString());
                                        }
                                    }

                                    for (int k = 0; k < abb_name.Count; k++)
                                    {
                                        char_ticssub += abb_name[k] + ":" + abb_value[k] + ";";
                                    }
                                }
                            }
                            else if (obj3.Name == "source" && obj3.Value.ValueKind == JsonValueKind.Object)
                                sourcesub = obj3.Value.GetProperty("shortName").GetString() ?? "";

                            else if (obj3.Name == "description")
                                descsub = obj3.Value.GetString() ?? "";

                            else if (obj3.Name == "speed" && obj3.Value.ValueKind == JsonValueKind.Array)
                            {
                                var aids = new List<string>();
                                var aids_value = new List<string>();
                                foreach (var speeds in obj3.Value.EnumerateArray())
                                {
                                    foreach (var speedwagon in speeds.EnumerateObject())
                                    {
                                        if (speedwagon.Name == "name")
                                        {
                                            switch (speedwagon.Value.GetString())
                                            {
                                                case "летая": aids.Add("лет"); break;
                                                case "плавая": aids.Add("пла"); break;
                                            }
                                        }
                                        else if (speedwagon.Name == "value")
                                        {
                                            aids_value.Add(speedwagon.Value.ToString());
                                        }
                                    }
                                    for (int k = 0; k < aids.Count; k++)
                                    {
                                        speedsub += aids[k] + ":" + aids_value[k + 1] + ";";
                                    }


                                }
                                speedsub += "пеш:" + aids_value[0] + ";";
                            }

                            else if (obj3.Name == "skills" && obj3.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var skill in obj3.Value.EnumerateArray())
                                {
                                    foreach (var skillProp in skill.EnumerateObject())
                                    {
                                        if (skillProp.Name == "name")
                                            skillsub += skillProp.Value.GetString() + ": ";
                                        else if (skillProp.Name == "description")
                                            skillsub += skillProp.Value.GetString() + "\n\n";
                                    }
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(namesub))
                    {
                        var newSpecies = new Species
                        {
                            Name = namesub,
                            SpeciesChaTics = char_ticssub,
                            SpeciesSkills = skillsub,
                            Description = descsub,
                            Source = sourcesub,
                            Speed = speedsub
                        };

                        if (!speciesToAdd.Any(s => s.Name == namesub))
                        {
                            speciesToAdd.Add(newSpecies);
                            Interlocked.Increment(ref count_race);
                            Console.WriteLine($"[УСПЕХ] Добавлена subrace: {namesub}");
                        }
                    }
                }
            }
        }

        private async Task ProcessMainRace(JsonElement property, ConcurrentBag<Species> speciesToAdd, int count)
        {
            string name = "";
            string skill = "";
            string char_tics = "";
            string source = "";
            string speed = "";
            string desc = "";
            foreach (var raceDocument in property.EnumerateObject())
            {
                if (raceDocument.Name == "name" && raceDocument.Value.ValueKind == JsonValueKind.Object)
                {
                    name = raceDocument.Value.GetProperty("rus").GetString() ?? "";
                }
                else if (raceDocument.Name == "abilities" && raceDocument.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ability in raceDocument.Value.EnumerateArray())
                    {
                        var abb_name = new List<string>();
                        var abb_value = new List<string>();

                        foreach (var abilityProp in ability.EnumerateObject())
                        {
                            if (abilityProp.Name == "name")
                            {
                                switch (abilityProp.Value.GetString())
                                {
                                    case "Сила": abb_name.Add("сил"); break;
                                    case "Ловкость": abb_name.Add("лов"); break;
                                    case "Телосложение": abb_name.Add("тел"); break;
                                    case "Интеллект": abb_name.Add("инт"); break;
                                    case "Мудрость": abb_name.Add("муд"); break;
                                    case "Харизма": abb_name.Add("хар"); break;
                                    case "+2 и +1 / +1 к трем": abb_name.Add("21/111"); break;
                                    case "к другой": abb_name.Add("другой"); break;
                                    case "к 2 другим": abb_name.Add("2другим"); break;
                                }
                            }
                            else if (abilityProp.Name == "value")
                            {
                                abb_value.Add(abilityProp.Value.ToString());
                            }
                        }

                        for (int k = 0; k < abb_name.Count; k++)
                        {
                            char_tics += abb_name[k] + ":" + abb_value[k] + ";";
                        }
                    }
                }
                else if (raceDocument.Name == "source" && raceDocument.Value.ValueKind == JsonValueKind.Object)
                {
                    source = raceDocument.Value.GetProperty("shortName").GetString() ?? "";
                }
                else if (raceDocument.Name == "subraces")
                {
                    await ProcessSubraces(raceDocument, speciesToAdd, count);
                }
                else if (raceDocument.Name == "description")
                {
                    desc = raceDocument.Value.GetString() ?? "";
                }
                else if (raceDocument.Name == "speed" && raceDocument.Value.ValueKind == JsonValueKind.Array)
                {
                    var aids = new List<string>();
                    var aids_value = new List<string>();
                    foreach (var speeds in raceDocument.Value.EnumerateArray())
                    {
                        foreach (var speedwagon in speeds.EnumerateObject())
                        {
                            if (speedwagon.Name == "name")
                            {
                                switch (speedwagon.Value.GetString())
                                {
                                    case "летая": aids.Add("лет"); break;
                                    case "плавая": aids.Add("пла"); break;
                                    default: break;
                                }
                            }
                            else if (speedwagon.Name == "value")
                            {
                                aids_value.Add(speedwagon.Value.ToString());
                            }
                        }
                        for (int k = 0; k < aids.Count; k++)
                        {
                            speed += aids[k] + ":" + aids_value[k + 1] + ";";
                        }


                    }
                    speed += "пеш:" + aids_value[0] + ";";
                }

                else if (raceDocument.Name == "skills" && raceDocument.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var skillItem in raceDocument.Value.EnumerateArray())
                    {
                        foreach (var skillProp in skillItem.EnumerateObject())
                        {
                            if (skillProp.Name == "name")
                                skill += skillProp.Value.GetString() + ": ";
                            else if (skillProp.Name == "description")
                                skill += skillProp.Value.GetString() + "\n\n";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                var newSpecies = new Species
                {
                    Name = name,
                    SpeciesChaTics = char_tics,
                    SpeciesSkills = skill,
                    Description = desc,
                    Source = source,
                    Speed = speed
                };

                // Thread-safe добавление
                if (!speciesToAdd.Any(s => s.Name == name))
                {
                    speciesToAdd.Add(newSpecies);
                    Interlocked.Increment(ref count_race);
                    Console.WriteLine($"[УСПЕХ] Добавлена основная раса: {name}");
                }
            }
        }
    }
}
