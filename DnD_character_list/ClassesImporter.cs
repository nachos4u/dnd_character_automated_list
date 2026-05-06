using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Text;
using System.Text.Json;

namespace DnD_character_list
{
    internal class ClassesImporter
    {
        // ──────────────────────────────────────────────────────────────
        // Entry point
        // ──────────────────────────────────────────────────────────────

        public async Task ImportClassAsync()
        {
            try
            {
                await SeedHitDiceAsync();

                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false,
                    Timeout = 60000
                });

                var page = await browser.NewPageAsync();

                // Register BEFORE GotoAsync — the class list API fires synchronously during page load
                var listResponseTask = page.WaitForResponseAsync(
                    r => IsClassListUrl(r.Url),
                    new() { Timeout = 60000 });

                await page.GotoAsync("https://5e14.ttg.club/classes");

                var listResponse = await listResponseTask;
                var listJson = await listResponse.TextAsync();
                // No NetworkIdle — SPA with WebSocket never reaches it.
                // We already have the data we need from the API response above.

                var classInfoList = ParseClassInfoList(listJson);

                foreach (var classInfo in classInfoList)
                {
                    try
                    {
                        await ImportOneClassAsync(page, classInfo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ОШИБКА] {classInfo.RusName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА] ImportClassAsync: {ex}");
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Seed hit dice reference table
        // ──────────────────────────────────────────────────────────────

        private async Task SeedHitDiceAsync()
        {
            using var db = new DDInformationContext();
            if (db.HitDices.Any()) return;
            db.HitDices.AddRange(
                new HitDice { HitDice1 = "к6" },
                new HitDice { HitDice1 = "к8" },
                new HitDice { HitDice1 = "к10" },
                new HitDice { HitDice1 = "к12" }
            );
            await db.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────────────────────
        // Step 1 – intercept /api/v1/classes list
        // ──────────────────────────────────────────────────────────────

        private record ArchetypeInfo(string Name, string Url);
        private record ClassInfo(string Slug, string RusName, List<ArchetypeInfo> BasicArchetypes);

        private List<ClassInfo> ParseClassInfoList(string jsonText)
        {
            var result = new List<ClassInfo>();
            var document = JsonSerializer.Deserialize<JsonElement>(jsonText);
            if (document.ValueKind != JsonValueKind.Array) return result;

            foreach (var item in document.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || !IsBasicSource(item)) continue;

                var url = item.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(url)) continue;

                var slug = url.Split('/').Last();
                var rusName = item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.Object
                    ? nameEl.GetProperty("rus").GetString() ?? "" : "";

                var archetypes = new List<ArchetypeInfo>();
                if (item.TryGetProperty("archetypes", out var archArr) && archArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arch in archArr.EnumerateArray())
                    {
                        if (!IsBasicSource(arch)) continue;
                        var archName = arch.TryGetProperty("name", out var aN) && aN.ValueKind == JsonValueKind.Object
                            ? aN.GetProperty("rus").GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(archName))
                            archetypes.Add(new ArchetypeInfo(archName, ""));
                    }
                }

                if (!string.IsNullOrEmpty(rusName))
                    result.Add(new ClassInfo(slug, rusName, archetypes));
            }

            return result;
        }

        private static bool IsClassListUrl(string url)
        {
            // Matches .../api/v1/classes or .../api/v1/classes?...
            // but NOT .../api/v1/classes/bard
            var idx = url.IndexOf("api/v1/classes", StringComparison.Ordinal);
            if (idx < 0) return false;
            var after = url[(idx + "api/v1/classes".Length)..];
            return after == "" || after.StartsWith("?");
        }

        private bool IsBasicSource(JsonElement item)
        {
            if (!item.TryGetProperty("source", out var src) || src.ValueKind != JsonValueKind.Object)
                return false;
            if (src.TryGetProperty("group", out var grp) &&
                grp.ValueKind == JsonValueKind.Object &&
                grp.TryGetProperty("shortName", out var sn))
                return sn.GetString() == "Basic";
            return false;
        }

        // ──────────────────────────────────────────────────────────────
        // Step 2 – import one class (2 page visits)
        // ──────────────────────────────────────────────────────────────

        private async Task ImportOneClassAsync(IPage page, ClassInfo classInfo)
        {
            if (classInfo.BasicArchetypes.Count == 0)
            {
                Console.WriteLine($"[ПРОПУСК] {classInfo.RusName} — нет базовых архетипов.");
                return;
            }

            // Visit 1: main class page → intercept API JSON for description
            string description = await GetClassDescriptionAsync(page, classInfo.Slug);

            // Visit 2: fragment page → parse HTML
            // GotoAsync waits for the load event. Then we wait for a key element
            // to confirm the server-rendered content is present before parsing.
            await page.GotoAsync($"https://5e14.ttg.club/classes/fragment/{classInfo.Slug}");
            await page.WaitForSelectorAsync("table tbody tr", new() { Timeout = 20000 });

            string hitDiceName = await ParseHitDiceAsync(page);
            string possession = await ParsePossessionAsync(page);
            var levelRows = await ParseLevelTableAsync(page);
            var featureDescriptions = await ParseFeatureDescriptionsAsync(page);
            var levelSkills = await ParseLevelSkillsAsync(page, featureDescriptions);
            var archetypeFeatures = await ParseArchetypeFeatureLevelsAsync(page, classInfo.BasicArchetypes);

            int idHitDice = GetOrCreateHitDiceId(hitDiceName);

            foreach (var arch in classInfo.BasicArchetypes)
                await UpsertArchetypeAsync(classInfo, arch, description, possession, idHitDice, levelRows, levelSkills, archetypeFeatures);
        }

        // ──────────────────────────────────────────────────────────────
        // Description from /classes/{slug} → /api/v1/classes/{slug}
        // ──────────────────────────────────────────────────────────────

        private async Task<string> GetClassDescriptionAsync(IPage page, string slug)
        {
            try
            {
                // Register BEFORE navigation so we don't miss the response
                var responseTask = page.WaitForResponseAsync(
                    r => r.Url.Contains("api/v1/classes") && r.Url.Contains(slug) && !IsClassListUrl(r.Url),
                    new() { Timeout = 30000 });

                // GotoAsync waits for the load event. The API call fires during Vue init,
                // so the response is captured by responseTask while the page loads.
                await page.GotoAsync($"https://5e14.ttg.club/classes/{slug}");

                await Task.Delay(15000);

                var apiResponse = await responseTask;
                var json = await apiResponse.TextAsync();
                var doc = JsonSerializer.Deserialize<JsonElement>(json);

                if (doc.TryGetProperty("description", out var descEl))
                    return StripHtml(descEl.GetString() ?? "");
            }
            catch { }

            return "";
        }

        // ──────────────────────────────────────────────────────────────
        // Hit dice from fragment page "Хиты" section
        // Structure: <details><summary class="h4"><span>Хиты</span></summary>
        //              <div class="content"><p class="class_stats">
        //                <b>Кость Хитов:</b> <dice-roller formula="1к8"></dice-roller>
        // ──────────────────────────────────────────────────────────────

        private async Task<string> ParseHitDiceAsync(IPage page)
        {
            // Search ONLY inside the "Хиты" section to avoid false positives —
            // e.g. bard body text contains "к12" in Bardic Inspiration description.
            return await page.EvaluateAsync<string>(@"() => {
                const summaries = Array.from(document.querySelectorAll('summary'));
                const h = summaries.find(el => /хиты/i.test(el.textContent));
                if (!h) return 'к8';
                const content = h.parentElement.querySelector('div.content');
                if (!content) return 'к8';
                const dr = content.querySelector('dice-roller');
                if (dr) {
                    const formula = dr.getAttribute('formula') || '';
                    const m = formula.match(/к(6|8|10|12)/);
                    if (m) return 'к' + m[1];
                }
                // Fallback: read innerText of the section paragraphs
                const statsText = Array.from(content.querySelectorAll('p.class_stats'))
                    .map(p => p.innerText).join(' ');
                const m2 = statsText.match(/к(6|8|10|12)/);
                return m2 ? 'к' + m2[1] : 'к8';
            }") ?? "к8";
        }

        // ──────────────────────────────────────────────────────────────
        // Possession from fragment page "Владение" section
        // Structure: <details><summary class="h4"><span>Владение</span></summary>
        //              <div class="content"><p class="class_stats"><b>Доспехи:</b>...
        // ──────────────────────────────────────────────────────────────

        private async Task<string> ParsePossessionAsync(IPage page)
        {
            return await page.EvaluateAsync<string>(@"() => {
                const summaries = Array.from(document.querySelectorAll('summary'));
                const h = summaries.find(el => /владени/i.test(el.textContent));
                if (!h) return '';
                const content = h.parentElement.querySelector('div.content');
                if (!content) return '';
                const parts = Array.from(content.querySelectorAll('p.class_stats'))
                    .map(p => p.innerText.trim())
                    .filter(Boolean);
                return parts.join('\n');
            }") ?? "";
        }

        // ──────────────────────────────────────────────────────────────
        // Level table
        // ──────────────────────────────────────────────────────────────

        private record LevelRow(int Level, string? Cells, string? ClassTableFeatures);

        private async Task<List<LevelRow>> ParseLevelTableAsync(IPage page)
        {
            var rows = new List<LevelRow>();

            // The bard table has a colspan=9 group header "Ячейки заклинаний на уровень заклинаний"
            // in the first thead row. Filtering it out keeps column indices aligned with data cells.
            var headers = await page.EvaluateAsync<string[]>(@"() => {
                const cells = Array.from(document.querySelectorAll('table thead th, table thead td'));
                return cells
                    .filter(el => parseInt(el.getAttribute('colspan') || '1') <= 1)
                    .map(el => el.innerText.trim());
            }") ?? [];

            int Col(params string[] keywords) =>
                Array.FindIndex(headers, h => keywords.Any(k => h.Contains(k, StringComparison.OrdinalIgnoreCase)));

            int profCol          = Col("Бонус мастерства", "БМ");
            int cantripsCol      = Col("Известные заговоры", "КЗ");
            int knownSpellsCol   = Col("Известные заклинания", "ИЗ");
            int sorceryCol       = Col("Единицы чародейства", "ЕЧ");
            int warlockSlotsCol  = Col("Ячейки заклинаний");
            int warlockSlotLvlCol = Col("Уровень ячеек", "УЯ");
            int invocationsCol   = Col("Воззвания", "ВЗ");

            var spellCols = new int[9];
            for (int i = 1; i <= 9; i++)
                spellCols[i - 1] = Array.FindIndex(headers, h => h.Trim() == i.ToString());

            var extraCols = new Dictionary<string, (int idx, string abbr)>();
            (string name, string abbr)[] extras =
            {
                ("Ярость",               "ЯР"),
                ("Урон ярости",          "УЯР"),
                ("Боевые искусства",     "БИ"),
                ("Очки ци",              "ОЦ"),
                ("Скорость без доспехов","СБД"),
                ("Скрытая атака",        "СА")
            };
            foreach (var (name, abbr) in extras)
            {
                int idx = Array.FindIndex(headers, h => h.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) extraCols[name] = (idx, abbr);
            }

            var tableRows = await page.QuerySelectorAllAsync("table tbody tr");
            int levelNum = 1;

            foreach (var row in tableRows)
            {
                if (levelNum > 20) break;

                var cells = await row.QuerySelectorAllAsync("td");
                if (cells.Count == 0) continue;

                var vals = new string[cells.Count];
                for (int i = 0; i < cells.Count; i++)
                    vals[i] = (await cells[i].InnerTextAsync()).Trim();

                string? GetVal(int col) =>
                    col >= 0 && col < vals.Length && vals[col] is { } v && v != "—" && v != "-" ? v : null;

                var cellsSb = new StringBuilder();
                void Append(string key, int col) { var v = GetVal(col); if (v != null) cellsSb.Append($"{key}:{v};"); }

                Append("БМ",  profCol);
                Append("КЗ",  cantripsCol);
                Append("ИЗ",  knownSpellsCol);
                Append("ЕЧ",  sorceryCol);
                Append("ЯЧ",  warlockSlotsCol);
                Append("УЯ",  warlockSlotLvlCol);
                Append("ВЗ",  invocationsCol);
                for (int i = 0; i < 9; i++) Append($"{i + 1}", spellCols[i]);

                var featuresSb = new StringBuilder();
                foreach (var (_, (idx, abbr)) in extraCols)
                {
                    var v = GetVal(idx);
                    if (v != null) featuresSb.Append($"{abbr}:{v};");
                }

                rows.Add(new LevelRow(
                    levelNum,
                    cellsSb.Length > 0 ? cellsSb.ToString() : null,
                    featuresSb.Length > 0 ? featuresSb.ToString() : null
                ));
                levelNum++;
            }

            return rows;
        }

        // ──────────────────────────────────────────────────────────────
        // Feature descriptions
        // Structure: <details>
        //              <summary class="h4" id="c1">
        //                <span>Вдохновение барда</span>
        //                <span class="source-data tip">PHB</span>
        //              </summary>
        //              <div class="content">
        //                <div class="caption_text"><span>1-й уровень...</span></div>
        //                <div><p>Описание...</p></div>
        //              </div>
        //            </details>
        // ──────────────────────────────────────────────────────────────

        private async Task<Dictionary<string, string>> ParseFeatureDescriptionsAsync(IPage page)
        {
            // id is on <summary>, not <details>
            var result = new Dictionary<string, string>();
            var summaries = await page.QuerySelectorAllAsync("summary[id]");

            foreach (var summary in summaries)
            {
                var id = await summary.GetAttributeAsync("id");
                if (string.IsNullOrEmpty(id)) continue;

                var text = await summary.EvaluateAsync<string>(@"el => {
                    const nameSpan = Array.from(el.querySelectorAll('span'))
                        .find(s => !s.classList.contains('source-data'));
                    const name = nameSpan
                        ? nameSpan.textContent.trim()
                        : el.textContent.replace(/\s+[A-Z]{2,6}$/, '').trim();

                    const details = el.parentElement;
                    const content = details.querySelector('div.content');
                    if (!content) return name;

                    const divs = Array.from(content.querySelectorAll(':scope > div'));
                    const descDiv = divs.find(d => !d.classList.contains('caption_text'));
                    const ps = descDiv
                        ? Array.from(descDiv.querySelectorAll('p')).map(p => p.innerText.trim()).filter(Boolean)
                        : Array.from(content.querySelectorAll('p'))
                            .filter(p => !p.closest('.caption_text'))
                            .map(p => p.innerText.trim()).filter(Boolean);

                    return ps.length ? name + '\n' + ps.join('\n') : name;
                }") ?? "";

                if (!string.IsNullOrEmpty(text))
                    result[id] = text;
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────
        // Level skills: feature links in table rows <a href="#c1">
        // ──────────────────────────────────────────────────────────────

        private record LevelSkillEntry(int Level, string Text);

        private async Task<List<LevelSkillEntry>> ParseLevelSkillsAsync(
            IPage page, Dictionary<string, string> featureDescriptions)
        {
            var result = new List<LevelSkillEntry>();
            var tableRows = await page.QuerySelectorAllAsync("table tbody tr");
            int levelNum = 1;

            foreach (var row in tableRows)
            {
                if (levelNum > 20) break;

                var links = await row.QuerySelectorAllAsync("a[href^='#c']");
                var sb = new StringBuilder();

                foreach (var link in links)
                {
                    var href = (await link.GetAttributeAsync("href") ?? "").TrimStart('#');
                    if (!string.IsNullOrEmpty(href) && featureDescriptions.TryGetValue(href, out var desc))
                        sb.AppendLine(desc);
                    else
                    {
                        var linkText = (await link.InnerTextAsync()).Trim();
                        if (!string.IsNullOrEmpty(linkText)) sb.AppendLine(linkText);
                    }
                }

                if (sb.Length > 0)
                    result.Add(new LevelSkillEntry(levelNum, sb.ToString().TrimEnd()));

                levelNum++;
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────
        // Archetype features
        // Structure: <details class="archetype_item">
        //              <summary class="h4"><span>Коллегия доблести</span></summary>
        //              <div class="content">
        //                <div><p>Intro...</p></div>
        //                <div>
        //                  <h4 class="header_separator"><span>Feature name</span></h4>
        //                  <p class="caption_text">3-ий уровень</p>
        //                  <div><p>Description...</p></div>
        //                </div>
        //              </div>
        //            </details>
        // ──────────────────────────────────────────────────────────────

        private record ArchetypeFeatureEntry(string ArchetypeName, int Level, string Text);

        private async Task<List<ArchetypeFeatureEntry>> ParseArchetypeFeatureLevelsAsync(
            IPage page, List<ArchetypeInfo> archetypes)
        {
            if (archetypes.Count == 0) return new();

            var archetypeNames = archetypes.Select(a => a.Name).ToArray();

            var jsonResult = await page.EvaluateAsync<string>(@"(names) => {
                const result = [];

                function normalize(s) {
                    return s.trim().replace(/\s+[A-Z]{2,6}$/, '').trim().toLowerCase();
                }

                const normalizedNames = names.map(normalize);
                const archetypeItems = Array.from(document.querySelectorAll('details.archetype_item'));

                for (const details of archetypeItems) {
                    const summary = details.querySelector('summary');
                    if (!summary) continue;
                    const nameSpan = Array.from(summary.querySelectorAll('span'))
                        .find(s => !s.classList.contains('source-data'));
                    if (!nameSpan) continue;

                    const archNorm = normalize(nameSpan.textContent);
                    const matchIdx = normalizedNames.findIndex(n =>
                        archNorm === n || archNorm.includes(n) || n.includes(archNorm));
                    if (matchIdx === -1) continue;

                    const originalName = names[matchIdx];
                    const content = details.querySelector('div.content');
                    if (!content) continue;

                    for (const h4 of content.querySelectorAll('h4.header_separator')) {
                        const featSpan = h4.querySelector('span');
                        const featName = featSpan ? featSpan.textContent.trim() : h4.textContent.trim();

                        const captionP = h4.nextElementSibling;
                        if (!captionP || !captionP.classList.contains('caption_text')) continue;

                        const m = captionP.textContent.match(/(\d+)/);
                        const level = m ? parseInt(m[1]) : 0;
                        if (level === 0) continue;

                        const descDiv = captionP.nextElementSibling;
                        const descLines = descDiv
                            ? Array.from(descDiv.querySelectorAll('p'))
                                .map(p => p.innerText.trim()).filter(Boolean)
                            : [];

                        result.push({
                            archName: originalName,
                            level,
                            text: featName + (descLines.length ? '\n' + descLines.join('\n') : '')
                        });
                    }
                }

                return JSON.stringify(result);
            }", archetypeNames) ?? "[]";

            var entries = new List<ArchetypeFeatureEntry>();
            try
            {
                var arr = JsonSerializer.Deserialize<JsonElement>(jsonResult);
                foreach (var item in arr.EnumerateArray())
                {
                    var archName = item.GetProperty("archName").GetString() ?? "";
                    var level    = item.GetProperty("level").GetInt32();
                    var text     = item.GetProperty("text").GetString() ?? "";
                    if (!string.IsNullOrEmpty(archName) && level > 0)
                        entries.Add(new ArchetypeFeatureEntry(archName, level, text));
                }
            }
            catch { }

            return entries;
        }

        // ──────────────────────────────────────────────────────────────
        // Upsert: update existing Class + delete/recreate its Levels
        // ──────────────────────────────────────────────────────────────

        private async Task UpsertArchetypeAsync(
            ClassInfo classInfo, ArchetypeInfo arch,
            string description, string mainPossession, int idHitDice,
            List<LevelRow> levelRows, List<LevelSkillEntry> levelSkills,
            List<ArchetypeFeatureEntry> archetypeFeatures)
        {
            var fullName = $"{classInfo.RusName}: {arch.Name}";

            var archExtraProficiency = archetypeFeatures
                .Where(f => f.ArchetypeName == arch.Name &&
                            (f.Text.Contains("навык", StringComparison.OrdinalIgnoreCase) ||
                             f.Text.Contains("владени", StringComparison.OrdinalIgnoreCase)))
                .Select(f => f.Text)
                .FirstOrDefault();

            var possession = string.IsNullOrEmpty(archExtraProficiency)
                ? mainPossession
                : mainPossession + "\n\nДополнительные владения архетипа:\n" + archExtraProficiency;

            using var db = new DDInformationContext();

            var existingClass = db.Classes
                .Include(c => c.Levels)
                .FirstOrDefault(c => c.Name == fullName);

            int idClass;
            if (existingClass != null)
            {
                existingClass.Description = description;
                existingClass.Possession  = possession;
                existingClass.IdHitDice   = idHitDice;
                db.Levels.RemoveRange(existingClass.Levels);
                await db.SaveChangesAsync();
                idClass = existingClass.IdClass;
            }
            else
            {
                var cls = new Class
                {
                    Name        = fullName,
                    Description = description,
                    Possession  = possession,
                    IdHitDice   = idHitDice
                };
                db.Classes.Add(cls);
                await db.SaveChangesAsync();
                idClass = cls.IdClass;
            }

            var archLevelFeatures = archetypeFeatures
                .Where(f => f.ArchetypeName == arch.Name)
                .GroupBy(f => f.Level)
                .ToDictionary(g => g.Key, g => string.Join("\n\n", g.Select(x => x.Text)));

            for (int lvl = 1; lvl <= 20; lvl++)
            {
                var row = levelRows.FirstOrDefault(r => r.Level == lvl);

                var skillsSb = new StringBuilder();
                var mainSkill = levelSkills.FirstOrDefault(s => s.Level == lvl);
                if (mainSkill != null) skillsSb.AppendLine(mainSkill.Text);
                if (archLevelFeatures.TryGetValue(lvl, out var archSkill)) skillsSb.AppendLine(archSkill);

                db.Levels.Add(new Level
                {
                    Level1             = lvl,
                    IdClass            = idClass,
                    Cells              = row?.Cells,
                    Skills             = skillsSb.Length > 0 ? skillsSb.ToString().TrimEnd() : null,
                    ClassTableFeatures = row?.ClassTableFeatures
                });
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"[УСПЕХ] {fullName}");
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        private static int GetOrCreateHitDiceId(string hitDiceName)
        {
            using var db = new DDInformationContext();
            var hd = db.HitDices.FirstOrDefault(h => h.HitDice1 == hitDiceName);
            if (hd != null) return hd.IdHitDice;
            hd = new HitDice { HitDice1 = hitDiceName };
            db.HitDices.Add(hd);
            db.SaveChanges();
            return hd.IdHitDice;
        }

        // Fix 7: убираем HTML-разметку, оставляем текст
        private static string StripHtml(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]+>", "");
    }
}
