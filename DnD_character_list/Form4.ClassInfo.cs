using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Вспомогательные статические методы ───────────────────────────────────

        private static Dictionary<string, string> ParseCells(string cells)
        {
            var d = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(cells)) return d;
            foreach (var part in cells.Split(';'))
            {
                var kv = part.Split(':', 2);
                if (kv.Length == 2 && kv[0].Length > 0)
                    d[kv[0].Trim()] = kv[1].Trim();
            }
            return d;
        }

        private static string GetBaseClassName(string name) => name.Split(':')[0].Trim();

        private static int GetCasterLevel(string className, int classLevel) =>
            GetBaseClassName(className) switch
            {
                "Бард" or "Жрец" or "Друид" or "Чародей" or "Волшебник" => classLevel,
                "Паладин" or "Следопыт"                                  => classLevel / 2,
                "Плут"    or "Воин"                                      => classLevel / 3,
                _ => 0
            };

        private static int GetDivineChannelCount(string className, int classLevel) =>
            GetBaseClassName(className) switch
            {
                "Жрец"   when classLevel >= 18 => 3,
                "Жрец"   when classLevel >= 6  => 2,
                "Жрец"   when classLevel >= 2  => 1,
                "Паладин" when classLevel >= 3 => 1,
                _ => 0
            };

        private static string GetMulticlassEquipment(string className) =>
            GetBaseClassName(className) switch
            {
                "Бард"     => "Лёгкие доспехи, 1 навык, 1 муз. инструмент",
                "Жрец"     => "Лёгкие и средние доспехи, щиты",
                "Друид"    => "Лёгкие и средние доспехи, щиты",
                "Воин"     => "Лёгкие и средние доспехи, щиты, простое и воинское оружие",
                "Следопыт" => "Лёгкие и средние доспехи, щиты, простое и воинское оружие",
                "Паладин"  => "Лёгкие и средние доспехи, щиты, простое и воинское оружие",
                "Плут"     => "Лёгкие доспехи, 1 навык, воровские инструменты",
                "Монах"    => "Простое оружие, короткие мечи",
                "Варвар"   => "Щиты, воинское оружие",
                "Колдун"   => "Лёгкие доспехи, простое оружие",
                _          => "—"
            };

        // ─── Заполнение всех полей класса ─────────────────────────────────────────
        private void FillClassInfo(Character character)
        {
            var cellLabels = new Label[]
            {
                CellLevel1Lable, CellLevel2Lable, CellLevel3Lable,
                CellLevel4Lable, CellLevel5Lable, CellLevel6Lable,
                CellLevel7Lable, CellLevel8Lable, CellLevel9Lable
            };

            // Инструменты из предыстории
            string bgTools = character.IdBackgroundNavigation?.ToolOwnership ?? "";
            var toolsSb = new StringBuilder();
            if (!string.IsNullOrEmpty(bgTools))
                toolsSb.AppendLine(bgTools.TrimEnd());

            if (!character.Levels.Any())
            {
                ToolsTextBox.Text  = toolsSb.ToString();
                FullClassTextBox.Text = "";
                if (FullClassTextBox2 != null) FullClassTextBox2.Text = "";
                ClassSkillsShortTextBox.Text = "";
                BMLable.Text = "";
                foreach (var lbl in cellLabels) lbl.Text = "";
                return;
            }

            // Группируем уровни по классу
            var classGroups = character.Levels
                .GroupBy(l => l.IdClass)
                .Select(g => new
                {
                    Class  = g.First().IdClassNavigation,
                    MaxLvl = g.Max(l => l.Level1),
                    Levels = g.OrderBy(l => l.Level1).ToList()
                })
                .ToList();

            bool isMulti = classGroups.Count > 1;
            int totalLvl = classGroups.Sum(g => g.MaxLvl);

            // Fix 3: Доспехи/Оружие/Инструменты первого класса (всегда, даже в мультиклассе)
            var firstCls = classGroups[0].Class;
            if (!string.IsNullOrEmpty(firstCls.Possession))
            {
                var classEquipSb = new StringBuilder();
                foreach (var pos in firstCls.Possession.Split('\n'))
                {
                    if (string.IsNullOrEmpty(pos)) continue;
                    var po = pos.Split(": ", 2);
                    if (po.Length < 2) continue;
                    var cat = po[0].Trim();
                    if (cat == "Доспехи" || cat == "Оружие" || cat == "Инструменты")
                        classEquipSb.AppendLine($"{cat}: {po[1].Trim()}");
                }
                if (classEquipSb.Length > 0)
                {
                    if (toolsSb.Length > 0) toolsSb.AppendLine();
                    toolsSb.AppendLine($"=== {GetBaseClassName(firstCls.Name)} ===");
                    toolsSb.Append(classEquipSb);
                }
            }
            ToolsTextBox.Text = toolsSb.ToString();

            // ── Бонус мастерства ─────────────────────────────────────────────────
            int bm = 2 + (totalLvl - 1) / 4;
            BMLable.Text = $"+{bm}";

            // ── Fix 6: FullClassTextBox (box1) + FullClassTextBox2 (box2) ─────────
            // Один класс → уровни 1-8 в box1, 9-20 в box2
            // Несколько классов → первая половина в box1, вторая в box2
            {
                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();
                bool singleClass = classGroups.Count == 1;
                int splitClassCount = (classGroups.Count + 1) / 2;

                for (int ci = 0; ci < classGroups.Count; ci++)
                {
                    var cg = classGroups[ci];
                    if (singleClass)
                    {
                        sb1.AppendLine($"=== {cg.Class.Name} (уровень {cg.MaxLvl}) — уровни 1–8 ===");
                        sb2.AppendLine($"=== {cg.Class.Name} (уровень {cg.MaxLvl}) — уровни 9–20 ===");
                        foreach (var lev in cg.Levels)
                        {
                            if (string.IsNullOrEmpty(lev.Skills)) continue;
                            var target = lev.Level1 <= 8 ? sb1 : sb2;
                            target.AppendLine($"— Уровень {lev.Level1} —");
                            target.AppendLine(lev.Skills);
                            target.AppendLine();
                        }
                    }
                    else
                    {
                        var targetSb = ci < splitClassCount ? sb1 : sb2;
                        targetSb.AppendLine($"=== {cg.Class.Name} (уровень {cg.MaxLvl}) ===");
                        foreach (var lev in cg.Levels)
                        {
                            if (!string.IsNullOrEmpty(lev.Skills))
                            {
                                targetSb.AppendLine($"— Уровень {lev.Level1} —");
                                targetSb.AppendLine(lev.Skills);
                                targetSb.AppendLine();
                            }
                        }
                    }
                }

                FullClassTextBox.Text = sb1.ToString();
                if (FullClassTextBox2 != null)
                    FullClassTextBox2.Text = sb2.ToString();
            }

            // ── ClassSkillsShortTextBox ───────────────────────────────────────────
            var featureNames = new Dictionary<string, string>
            {
                ["СА"]  = "Скрытая атака",       ["ЯР"]  = "Ярость",
                ["УЯР"] = "Урон ярости",          ["БИ"]  = "Боевые искусства",
                ["ОЦ"]  = "Очки ци",              ["СБД"] = "Скорость без доспехов",
                ["ЕЧ"]  = "Единицы чародейства",  ["ВЗ"]  = "Воззвания",
                ["КЗ"]  = "Заговоры",             ["ИЗ"]  = "Известные заклинания",
                ["ЯЧ"]  = "Ячейки (чернокн.)",   ["УЯ"]  = "Уровень ячеек (чернокн.)",
            };

            var featDict = new Dictionary<string, string>();
            int divineChannel = 0;

            foreach (var cg in classGroups)
            {
                divineChannel = Math.Max(divineChannel, GetDivineChannelCount(cg.Class.Name, cg.MaxLvl));
                foreach (var lev in cg.Levels)
                {
                    if (string.IsNullOrEmpty(lev.ClassTableFeatures)) continue;
                    foreach (var kv in ParseCells(lev.ClassTableFeatures))
                        featDict[kv.Key] = kv.Value;
                }
            }

            var shortSb = new StringBuilder();
            shortSb.AppendLine("— Особенности класса —");
            foreach (var kv in featDict)
            {
                string label = featureNames.TryGetValue(kv.Key, out string fn) ? fn : kv.Key;
                shortSb.AppendLine($"{label}: {kv.Value}");
            }
            if (divineChannel > 0)
                shortSb.AppendLine($"Божественный канал: {divineChannel} исп.");

            shortSb.AppendLine();
            shortSb.AppendLine("— Классовые умения —");
            var seen = new HashSet<string>();
            foreach (var cg in classGroups)
                foreach (var lev in cg.Levels)
                    if (!string.IsNullOrEmpty(lev.Skills))
                        foreach (var block in lev.Skills.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var title = block.Trim().Split('\n')[0].Trim();
                            if (!string.IsNullOrEmpty(title) && seen.Add(title))
                                shortSb.AppendLine(title);
                        }

            // ── Ячейки заклинаний ────────────────────────────────────────────────
            string warlockSlots = null, warlockSlotLvl = null;

            if (!isMulti)
            {
                // Fix 2: кружочки по количеству слотов
                var cells = ParseCells(classGroups[0].Levels.Last().Cells ?? "");
                for (int i = 1; i <= 9; i++)
                {
                    if (cells.TryGetValue(i.ToString(), out string v) &&
                        int.TryParse(v, out int cnt) && cnt > 0)
                        cellLabels[i - 1].Text = string.Join(" ", Enumerable.Repeat("○", cnt));
                    else
                        cellLabels[i - 1].Text = "";
                }
            }
            else
            {
                int effectiveCasterLvl = 0;
                foreach (var cg in classGroups)
                {
                    if (GetBaseClassName(cg.Class.Name) == "Колдун")
                    {
                        var wlCells = ParseCells(cg.Levels.Last().Cells ?? "");
                        wlCells.TryGetValue("ЯЧ", out warlockSlots);
                        wlCells.TryGetValue("УЯ", out warlockSlotLvl);
                    }
                    else
                        effectiveCasterLvl += GetCasterLevel(cg.Class.Name, cg.MaxLvl);
                }

                if (effectiveCasterLvl > 0)
                {
                    var slots = _multiclassSlotTable[Math.Clamp(effectiveCasterLvl, 1, 20) - 1];
                    for (int i = 0; i < 9; i++)
                        cellLabels[i].Text = slots[i] > 0
                            ? string.Join(" ", Enumerable.Repeat("○", slots[i]))
                            : "";
                }
                else
                    foreach (var lbl in cellLabels) lbl.Text = "";

                if (warlockSlots != null)
                    shortSb.AppendLine($"\nЯчейки чернокнижника: {warlockSlots} (ур. {warlockSlotLvl})");

                // Снаряжение мультикласса (ограниченное для дополнительных классов)
                var mcEquipSb = new StringBuilder();
                for (int i = 1; i < classGroups.Count; i++)
                {
                    string eq = GetMulticlassEquipment(classGroups[i].Class.Name);
                    if (eq != "—")
                        mcEquipSb.AppendLine($"{GetBaseClassName(classGroups[i].Class.Name)} (мультикласс): {eq}");
                }
                if (mcEquipSb.Length > 0)
                    ToolsTextBox.Text += "\n=== Мультикласс ===\n" + mcEquipSb;
            }

            ClassSkillsShortTextBox.Text = shortSb.ToString();
        }
    }
}
