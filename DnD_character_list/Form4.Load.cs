using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Загрузка персонажа ────────────────────────────────────────────────────
        void Load_character()
        {
            isLoading = true;
            using (var db = new DDInformationContext())
            {
                var character = db.Characters
                    .Include(c => c.Levels).ThenInclude(l => l.IdClassNavigation)
                    .Include(c => c.IdBackgroundNavigation)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                if (character == null)
                {
                    isLoading = false;
                    return;
                }

                if (character.Gm != null)         gmUpDown.Value       = character.Gm.Value;
                if (character.Mm != null)         mmUpDown.Value       = character.Mm.Value;
                if (character.Sm != null)         smUpDown.Value       = character.Sm.Value;
                if (character.Em != null)         emUpDown.Value       = character.Em.Value;
                if (character.Pm != null)         pmUpDown.Value       = character.Pm.Value;
                if (character.Name != null)       NameTextBox.Text     = character.Name;
                if (character.Hitpoints != null)  MaxHPUpDown.Value    = character.Hitpoints.Value;
                if (character.CurHp != null)      CurHPUpDown.Value    = character.CurHp.Value;
                if (character.Exp != null)        ExUpDown.Value       = character.Exp.Value;
                if (character.Notes != null)      NotesTextBox.Text    = character.Notes;
                if (character.Kd != null)         KDUpDown.Value       = character.Kd.Value;

                // Спасброски
                if (character.Description != null)
                {
                    foreach (string item in character.Description.Split(';'))
                    {
                        switch (item)
                        {
                            case "сил": StrengthCheckBox.Checked    = true; break;
                            case "лов": AgilityCheckBox.Checked     = true; break;
                            case "тел": StaminaCheckBox.Checked     = true; break;
                            case "инт": IntelligenceCheckBox.Checked = true; break;
                            case "муд": WisdomCheckBox.Checked      = true; break;
                            case "хар": CharismaCheckBox.Checked    = true; break;
                        }
                    }
                }

                // Комбо-боксы
                comboBackground.DataSource    = db.Backgrounds.ToList();
                comboBackground.DisplayMember = "Name";
                comboBackground.ValueMember   = "IdBackground";

                comboSpecies.DataSource    = db.Species.ToList();
                comboSpecies.DisplayMember = "Name";
                comboSpecies.ValueMember   = "IdSpecies";

                // Навыки
                if (character.PossesionNew != null)
                {
                    foreach (string item in character.PossesionNew.Split(';'))
                    {
                        switch (item)
                        {
                            case "атлетика":           AthleticsCheckBox.Checked = true;     break;
                            case "акробатика":         AcrobaticsCheckBox.Checked = true;    break;
                            case "ловкость рук":       DexterityCheckBox.Checked = true;     break;
                            case "скрытность":         StealthCheckBox.Checked = true;       break;
                            case "анализ":             DessectionCheckBox.Checked = true;    break;
                            case "история":            HistoryCheckBox.Checked = true;       break;
                            case "магия":              MagicCheckBox.Checked = true;         break;
                            case "природа":            NatureCheckBox.Checked = true;        break;
                            case "религия":            ReligionCheckBox.Checked = true;      break;
                            case "восприятие":         PerceptionCheckBox.Checked = true;    break;
                            case "выживание":          SurvivalCheckBox.Checked = true;      break;
                            case "медицина":           MedicineCheckBox.Checked = true;      break;
                            case "проницание":         DiscriminationCheckBox.Checked = true; break;
                            case "уход за животными":  AnimalCareCheckBox.Checked = true;    break;
                            case "выступление":        PerformanceCheckBox.Checked = true;   break;
                            case "запугивание":        IntimidationCheckBox.Checked = true;  break;
                            case "обман":              DeceptionCheckBox.Checked = true;     break;
                            case "убеждение":          PersuasionCheckBox.Checked = true;    break;
                            case "атлетика+":          AthleticsCheckBox.CheckState      = CheckState.Indeterminate; break;
                            case "акробатика+":        AcrobaticsCheckBox.CheckState     = CheckState.Indeterminate; break;
                            case "ловкость рук+":      DexterityCheckBox.CheckState      = CheckState.Indeterminate; break;
                            case "скрытность+":        StealthCheckBox.CheckState        = CheckState.Indeterminate; break;
                            case "анализ+":            DessectionCheckBox.CheckState     = CheckState.Indeterminate; break;
                            case "история+":           HistoryCheckBox.CheckState        = CheckState.Indeterminate; break;
                            case "магия+":             MagicCheckBox.CheckState          = CheckState.Indeterminate; break;
                            case "природа+":           NatureCheckBox.CheckState         = CheckState.Indeterminate; break;
                            case "религия+":           ReligionCheckBox.CheckState       = CheckState.Indeterminate; break;
                            case "восприятие+":        PerceptionCheckBox.CheckState     = CheckState.Indeterminate; break;
                            case "выживание+":         SurvivalCheckBox.CheckState       = CheckState.Indeterminate; break;
                            case "медицина+":          MedicineCheckBox.CheckState       = CheckState.Indeterminate; break;
                            case "проницание+":        DiscriminationCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "уход за животными+": AnimalCareCheckBox.CheckState     = CheckState.Indeterminate; break;
                            case "выступление+":       PerformanceCheckBox.CheckState    = CheckState.Indeterminate; break;
                            case "запугивание+":       IntimidationCheckBox.CheckState   = CheckState.Indeterminate; break;
                            case "обман+":             DeceptionCheckBox.CheckState      = CheckState.Indeterminate; break;
                            case "убеждение+":         PersuasionCheckBox.CheckState     = CheckState.Indeterminate; break;
                        }
                    }
                }

                // Броски смерти
                if (character.SpasWin != null)
                {
                    switch (character.SpasWin)
                    {
                        case 1: WinCheck1.Checked = true; break;
                        case 2: WinCheck1.Checked = true; WinCheck2.Checked = true; break;
                        case 3: WinCheck1.Checked = true; WinCheck2.Checked = true; WinCheck3.Checked = true; break;
                    }
                }

                if (character.SpasLose != null)
                {
                    switch (character.SpasLose)
                    {
                        case 1: LoseCheck1.Checked = true; break;
                        case 2: LoseCheck1.Checked = true; LoseCheck2.Checked = true; break;
                        case 3: LoseCheck1.Checked = true; LoseCheck2.Checked = true; LoseCheck3.Checked = true; break;
                    }
                }

                if (character.Possession != null)
                    OtherSkillsTextBox.Text = character.Possession;

                // Характеристики
                if (character.Characteristiks != null)
                {
                    var stats = character.Characteristiks.Split(';');
                    if (stats.Length >= 6)
                    {
                        if (decimal.TryParse(stats[0], out decimal s0)) StrengthUpDown.Value     = s0;
                        if (decimal.TryParse(stats[1], out decimal s1)) AgilityUpDown.Value      = s1;
                        if (decimal.TryParse(stats[2], out decimal s2)) StaminaUpDown.Value      = s2;
                        if (decimal.TryParse(stats[3], out decimal s3)) IntelligenceUpDown.Value = s3;
                        if (decimal.TryParse(stats[4], out decimal s4)) WisdomUpDown.Value       = s4;
                        if (decimal.TryParse(stats[5], out decimal s5)) CharismaUpDown.Value     = s5;
                    }
                }

                comboSpecies.SelectedValue    = character.IdSpecies;
                comboBackground.SelectedValue = character.IdBackground;

                UpdateLevelButton(character);

                ClassNameTextBox.Text = string.Join("\n", character.Levels
                    .Select(l => l.IdClassNavigation?.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct());

                if (character.Speed != null && character.Speed != 0)
                    SpeedUpDown.Value = character.Speed.Value;

                // Вид (скорость, умения вида)
                var species = db.Species.FirstOrDefault(s => s.IdSpecies == character.IdSpecies);
                if (species != null)
                {
                    SpecieSkillsTextBox.Text = species.SpeciesSkills ?? "";
                    if (!string.IsNullOrEmpty(species.Speed))
                    {
                        foreach (var seg in species.Speed.Split(';'))
                        {
                            if (string.IsNullOrEmpty(seg)) continue;
                            var parts = seg.Split(':');
                            if (parts.Length < 2) continue;
                            switch (parts[0].Trim())
                            {
                                case "пла":
                                    SpeedSkillTextBox.Text = $"\n\n Ваша скорость плаванья составляет {parts[1]} футов";
                                    break;
                                case "лет":
                                    SpeedSkillTextBox.Text = $"\n\n Ваша скорость полёта составляет {parts[1]} футов";
                                    break;
                            }
                        }
                    }
                }

                // Предыстория
                var bg = db.Backgrounds.FirstOrDefault(b => b.IdBackground == character.IdBackground);
                if (bg != null)
                    BackgroundDescTextBox.Text = (bg.Description ?? "") + "\n \n" + bg.Invetary;

                CurDiceHPUpDown.Value = character.TimeHitpoints ?? 0;

                FillClassInfo(character);

                // Восстанавливаем анимацию навыков, если была активна
                if (character.SkillsPending && !string.IsNullOrEmpty(character.PendingSkillChoices))
                {
                    var skillList = character.PendingSkillChoices.Split(';').ToList();
                    StartSkillAnimation(skillList, character.PendingSkillCount ?? 2);
                }

                isLoading = false;

                // Заполняем вычисляемые значения после загрузки
                UpdateCalculatedValues();
            }
        }

        // ─── Смена вида ────────────────────────────────────────────────────────────
        private void comboSpecies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;

            var selectedSpecies = comboSpecies.SelectedItem as Species;
            if (selectedSpecies == null) return;

            int specieId = selectedSpecies.IdSpecies;

            try
            {
                using (var db = new DDInformationContext())
                {
                    var specie = db.Species
                        .Where(s => s.IdSpecies == specieId)
                        .Select(s => new { s.Speed, s.SpeciesChaTics, s.SpeciesSkills, s.IdSpecies })
                        .FirstOrDefault();

                    if (specie == null) return;

                    var character = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                    if (character == null) return;

                    character.IdSpecies = specieId;
                    SpecieSkillsTextBox.Text = specie.SpeciesSkills ?? "";

                    var prevSpecies = oldStats.getoldspecie(DataIDCharacter);

                    // Откатываем старые бонусы вида
                    if (!string.IsNullOrEmpty(prevSpecies?.Speed))
                    {
                        foreach (var seg in prevSpecies.Speed.Split(';'))
                        {
                            if (string.IsNullOrEmpty(seg)) continue;
                            var aids = seg.Split(':');
                            if (aids.Length < 2) continue;
                            switch (aids[0].Trim())
                            {
                                case "пеш":
                                    if (int.TryParse(aids[1], out int walkSpeed))
                                    { character.Speed = walkSpeed; SpeedUpDown.Value = walkSpeed; }
                                    break;
                                case "пла":
                                case "лет":
                                    SpeedSkillTextBox.Text = "";
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(prevSpecies?.SpeciesChaTics))
                    {
                        foreach (var chart in prevSpecies.SpeciesChaTics.Split(';'))
                        {
                            if (string.IsNullOrEmpty(chart)) continue;
                            var chartick = chart.Split(':');
                            if (chartick.Length < 2) continue;
                            switch (chartick[0].Trim())
                            {
                                case "сил": if (int.TryParse(chartick[1], out int s))  StrengthUpDown.Value     -= s; break;
                                case "лов": if (int.TryParse(chartick[1], out int a))  AgilityUpDown.Value      -= a; break;
                                case "тел": if (int.TryParse(chartick[1], out int st)) StaminaUpDown.Value      -= st; break;
                                case "инт": if (int.TryParse(chartick[1], out int i))  IntelligenceUpDown.Value -= i; break;
                                case "муд": if (int.TryParse(chartick[1], out int w))  WisdomUpDown.Value       -= w; break;
                                case "хар": if (int.TryParse(chartick[1], out int c))  CharismaUpDown.Value     -= c; break;
                            }
                        }
                    }

                    // Применяем новые бонусы вида
                    if (!string.IsNullOrEmpty(specie.Speed))
                    {
                        foreach (var seg in specie.Speed.Split(';'))
                        {
                            if (string.IsNullOrEmpty(seg)) continue;
                            var aids = seg.Split(':');
                            if (aids.Length < 2) continue;
                            switch (aids[0].Trim())
                            {
                                case "пеш":
                                    if (int.TryParse(aids[1], out int walkSpeed))
                                    { character.Speed = walkSpeed; SpeedUpDown.Value = walkSpeed; }
                                    break;
                                case "пла":
                                    SpeedSkillTextBox.Text = $"\n\n Ваша скорость плаванья составляет {aids[1]} футов";
                                    break;
                                case "лет":
                                    SpeedSkillTextBox.Text = $"\n\n Ваша скорость полёта составляет {aids[1]} футов";
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(specie.SpeciesChaTics))
                    {
                        foreach (var chart in specie.SpeciesChaTics.Split(';'))
                        {
                            if (string.IsNullOrEmpty(chart)) continue;
                            var chartick = chart.Split(':');
                            if (chartick.Length < 2) continue;
                            switch (chartick[0].Trim())
                            {
                                case "сил": if (int.TryParse(chartick[1], out int s))  StrengthUpDown.Value     += s; break;
                                case "лов": if (int.TryParse(chartick[1], out int a))  AgilityUpDown.Value      += a; break;
                                case "тел": if (int.TryParse(chartick[1], out int st)) StaminaUpDown.Value      += st; break;
                                case "инт": if (int.TryParse(chartick[1], out int i))  IntelligenceUpDown.Value += i; break;
                                case "муд": if (int.TryParse(chartick[1], out int w))  WisdomUpDown.Value       += w; break;
                                case "хар": if (int.TryParse(chartick[1], out int c))  CharismaUpDown.Value     += c; break;
                                case "21/111":
                                    MessageBox.Show("Вам надо будет выбрать: \n\n" +
                                        "2 очка в одну характеристику и 1 в другую \n" +
                                        "либо в 3 характеристики по 1");
                                    break;
                                case "другой":
                                    MessageBox.Show("Вам нужно добавить 1 очко к любой характеристике");
                                    break;
                                case "2другим":
                                    MessageBox.Show("Вам нужно добавить 1 очко к ДВУМ любым характеристикам");
                                    break;
                            }
                        }
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Смена предыстории ─────────────────────────────────────────────────────
        private void comboBackground_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;

            var selectedBackground = comboBackground.SelectedItem as Background;
            if (selectedBackground == null) return;

            int backgroundID = selectedBackground.IdBackground;
            try
            {
                using (var db = new DDInformationContext())
                {
                    var background = db.Backgrounds
                        .Where(b => b.IdBackground == backgroundID)
                        .Select(b => new { b.Name, b.Possesion, b.Invetary, b.Gm, b.Description, b.ToolOwnership })
                        .FirstOrDefault();

                    var charecter = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                    if (charecter == null) return;

                    charecter.IdBackground = backgroundID;

                    BackgroundDescTextBox.Text = (background.Description ?? "") + "\n \n" + background.Invetary;

                    var oldBg = oldStats.getoldbackground(DataIDCharacter);
                    int prev_gm = oldBg?.Gm ?? 0;

                    // Откатываем навыки старой предыстории
                    if (oldBg?.Possesion != null)
                    {
                        foreach (string item in oldBg.Possesion.Split(';'))
                        {
                            switch (item)
                            {
                                case "атлетика":          AthleticsCheckBox.Checked       = false; break;
                                case "акробатика":        AcrobaticsCheckBox.Checked      = false; break;
                                case "ловкость рук":      DexterityCheckBox.Checked       = false; break;
                                case "скрытность":        StealthCheckBox.Checked         = false; break;
                                case "анализ":            DessectionCheckBox.Checked      = false; break;
                                case "история":           HistoryCheckBox.Checked         = false; break;
                                case "магия":             MagicCheckBox.Checked           = false; break;
                                case "природа":           NatureCheckBox.Checked          = false; break;
                                case "религия":           ReligionCheckBox.Checked        = false; break;
                                case "восприятие":        PerceptionCheckBox.Checked      = false; break;
                                case "выживание":         SurvivalCheckBox.Checked        = false; break;
                                case "медицина":          MedicineCheckBox.Checked        = false; break;
                                case "проницательность":  DiscriminationCheckBox.Checked  = false; break;
                                case "уход за животными": AnimalCareCheckBox.Checked      = false; break;
                                case "выступление":       PerformanceCheckBox.Checked     = false; break;
                                case "запугивание":       IntimidationCheckBox.Checked    = false; break;
                                case "обман":             DeceptionCheckBox.Checked       = false; break;
                                case "убеждение":         PersuasionCheckBox.Checked      = false; break;
                            }
                        }
                    }

                    // Применяем навыки новой предыстории
                    if (background.Possesion != null)
                    {
                        foreach (string item in background.Possesion.Split(';'))
                        {
                            switch (item)
                            {
                                case "атлетика":          AthleticsCheckBox.Checked       = true; break;
                                case "акробатика":        AcrobaticsCheckBox.Checked      = true; break;
                                case "ловкость рук":      DexterityCheckBox.Checked       = true; break;
                                case "скрытность":        StealthCheckBox.Checked         = true; break;
                                case "анализ":            DessectionCheckBox.Checked      = true; break;
                                case "история":           HistoryCheckBox.Checked         = true; break;
                                case "магия":             MagicCheckBox.Checked           = true; break;
                                case "природа":           NatureCheckBox.Checked          = true; break;
                                case "религия":           ReligionCheckBox.Checked        = true; break;
                                case "восприятие":        PerceptionCheckBox.Checked      = true; break;
                                case "выживание":         SurvivalCheckBox.Checked        = true; break;
                                case "медицина":          MedicineCheckBox.Checked        = true; break;
                                case "проницательность":  DiscriminationCheckBox.Checked  = true; break;
                                case "уход за животными": AnimalCareCheckBox.Checked      = true; break;
                                case "выступление":       PerformanceCheckBox.Checked     = true; break;
                                case "запугивание":       IntimidationCheckBox.Checked    = true; break;
                                case "обман":             DeceptionCheckBox.Checked       = true; break;
                                case "убеждение":         PersuasionCheckBox.Checked      = true; break;
                            }
                        }
                    }

                    // Сохраняем состояние навыков
                    string possnew = null;
                    foreach (var poss in GetAllSkillCheckBoxes())
                    {
                        if (poss.CheckState == CheckState.Checked)
                            possnew += poss.Text.ToLower() + ";";
                        else if (poss.CheckState == CheckState.Indeterminate)
                            possnew += poss.Text.ToLower() + "+;";
                    }
                    charecter.PossesionNew = possnew;

                    if (background.ToolOwnership != null)
                        ToolsTextBox.Text = background.ToolOwnership;

                    int newGold = Math.Max(0, (int)gmUpDown.Value - prev_gm) + (background.Gm ?? 0);
                    gmUpDown.Value = newGold;
                    charecter.Gm = newGold;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка {ex.Message}");
            }
        }
    }
}
