using DnD_character_list;
using Microsoft.Playwright;
using System.Text;
using System.Text.Json;

namespace DnD_character_list
{
    internal class FeatsImporter
    {
        public async Task ImportFeatsAsync(
            IProgress<(int current, int total, string name)>? progress = null)
        {
            var proxies = new List<string>
            {
                // Add proxy strings here in format "ip:port@user:pass"
            };

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    Timeout  = 60000
                });

                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://5e14.ttg.club/feats");

                // Feats page loads all at once — single API response, no scroll needed
                var response = await page.WaitForResponseAsync(r =>
                    r.Url.Contains("api/v1/feats"), new() { Timeout = 60000 });

                var jsonText = await response.TextAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var document = JsonSerializer.Deserialize<JsonElement>(jsonText);
                var urls     = new List<string>();

                if (document.ValueKind == JsonValueKind.Array)
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

                Console.WriteLine($"[Черты] Найдено URL: {urls.Count}");

                int   total   = urls.Count;
                int[] counter = { 0 };

                await ProcessFeatsSequentially(urls, proxies, page, total, counter, progress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }

        private async Task ProcessFeatsSequentially(
            List<string> urls,
            List<string> proxies,
            IPage page,
            int total,
            int[] counter,
            IProgress<(int current, int total, string name)>? progress)
        {
            var savedNames = new HashSet<string>();

            foreach (var url in urls)
            {
                string? processedName = null;
                try
                {
                    await Task.Delay(Random.Shared.Next(13000, 15000));

                    var fullUrl = "https://5e14.ttg.club" + url;
                    await page.GotoAsync(fullUrl);

                    var apiUrl         = "/api/v1" + url;
                    var detailResponse = await page.WaitForResponseAsync(r =>
                        r.Url.Contains(apiUrl), new() { Timeout = 30000 });

                    var bytes    = await detailResponse.BodyAsync();
                    string jsonText = Encoding.UTF8.GetString(bytes);
                    var doc      = JsonSerializer.Deserialize<JsonElement>(jsonText);

                    if (doc.ValueKind == JsonValueKind.Object)
                    {
                        var trait = ParseFeatFromDocument(doc);
                        if (trait != null)
                        {
                            processedName = trait.Name;

                            if (!savedNames.Contains(trait.Name ?? ""))
                            {
                                savedNames.Add(trait.Name ?? "");
                                try
                                {
                                    using var db = new DDInformationContext();
                                    var existing = db.Traits.FirstOrDefault(t => t.Name == trait.Name);

                                    if (existing != null)
                                    {
                                        existing.Description  = trait.Description;
                                        existing.Requirements = trait.Requirements;
                                        existing.Source       = trait.Source;
                                        Console.WriteLine($"[ОБНОВЛЕНО] Черта: {trait.Name}");
                                    }
                                    else
                                    {
                                        db.Traits.Add(trait);
                                        Console.WriteLine($"[ДОБАВЛЕНО] Черта: {trait.Name}");
                                    }

                                    await db.SaveChangesAsync();
                                }
                                catch (Exception saveEx)
                                {
                                    Console.WriteLine($"[ОШИБКА] {trait.Name}: {saveEx.InnerException?.Message ?? saveEx.Message}");
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
        }

        private Trait? ParseFeatFromDocument(JsonElement doc)
        {
            string name         = "";
            string desc         = "";
            string requirements = "";
            string source       = "";

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

                    case "requirement":
                    case "requirements":
                        if (prop.Value.ValueKind == JsonValueKind.String)
                            requirements = StripHtml(prop.Value.GetString() ?? "");
                        else
                            requirements = StripHtml(prop.Value.ToString());
                        break;

                    case "source" when prop.Value.ValueKind == JsonValueKind.Object:
                        if (prop.Value.TryGetProperty("shortName", out var sn))
                            source = sn.GetString() ?? "";
                        break;
                }
            }

            if (string.IsNullOrEmpty(name)) return null;

            return new Trait
            {
                Name         = name,
                Description  = desc,
                Requirements = requirements,
                Source       = source,
                CharTics     = null   // legacy field not used by new records
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
