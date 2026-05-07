using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Перечисление всех чекбоксов навыков ──────────────────────────────────
        private IEnumerable<CheckBox> GetAllSkillCheckBoxes() =>
            new CheckBox[]
            {
                AthleticsCheckBox, AcrobaticsCheckBox, DexterityCheckBox, StealthCheckBox,
                DessectionCheckBox, HistoryCheckBox, MagicCheckBox, NatureCheckBox,
                ReligionCheckBox, PerceptionCheckBox, SurvivalCheckBox,
                MedicineCheckBox, DiscriminationCheckBox, AnimalCareCheckBox,
                PerformanceCheckBox, IntimidationCheckBox, DeceptionCheckBox, PersuasionCheckBox
            };

        // ─── Подписка на события UI для автосохранения ────────────────────────────
        private void WireAutoSave()
        {
            // Текстовые поля
            NameTextBox.TextChanged        += (s, e) => SaveCharacterToDb();
            NotesTextBox.TextChanged       += (s, e) => SaveCharacterToDb();
            OtherSkillsTextBox.TextChanged += (s, e) => SaveCharacterToDb();

            // Числовые поля (не характеристики)
            MaxHPUpDown.ValueChanged      += (s, e) => SaveCharacterToDb();
            CurHPUpDown.ValueChanged      += (s, e) => SaveCharacterToDb();
            ExUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            SpeedUpDown.ValueChanged      += (s, e) => SaveCharacterToDb();
            gmUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            mmUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            smUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            emUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            pmUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            KDUpDown.ValueChanged         += (s, e) => SaveCharacterToDb();
            CurDiceHPUpDown.ValueChanged  += (s, e) => SaveCharacterToDb();

            // Характеристики — сохраняем и пересчитываем производные
            StrengthUpDown.ValueChanged    += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };
            AgilityUpDown.ValueChanged     += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };
            StaminaUpDown.ValueChanged     += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };
            IntelligenceUpDown.ValueChanged += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };
            WisdomUpDown.ValueChanged      += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };
            CharismaUpDown.ValueChanged    += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };

            // Спасброски
            StrengthCheckBox.CheckedChanged    += (s, e) => SaveCharacterToDb();
            AgilityCheckBox.CheckedChanged     += (s, e) => SaveCharacterToDb();
            StaminaCheckBox.CheckedChanged     += (s, e) => SaveCharacterToDb();
            IntelligenceCheckBox.CheckedChanged += (s, e) => SaveCharacterToDb();
            WisdomCheckBox.CheckedChanged      += (s, e) => SaveCharacterToDb();
            CharismaCheckBox.CheckedChanged    += (s, e) => SaveCharacterToDb();

            // Броски смерти
            WinCheck1.CheckedChanged  += (s, e) => SaveCharacterToDb();
            WinCheck2.CheckedChanged  += (s, e) => SaveCharacterToDb();
            WinCheck3.CheckedChanged  += (s, e) => SaveCharacterToDb();
            LoseCheck1.CheckedChanged += (s, e) => SaveCharacterToDb();
            LoseCheck2.CheckedChanged += (s, e) => SaveCharacterToDb();
            LoseCheck3.CheckedChanged += (s, e) => SaveCharacterToDb();

            // Навыки — сохраняем; восприятие также пересчитывает пассивное восприятие
            PerceptionCheckBox.CheckedChanged += (s, e) => { SaveCharacterToDb(); UpdateCalculatedValues(); };
            foreach (var cb in GetAllSkillCheckBoxes())
            {
                if (cb != PerceptionCheckBox)
                    cb.CheckedChanged += (s, e) => SaveCharacterToDb();
            }
        }

        // ─── Сохранение персонажа в БД ────────────────────────────────────────────
        private void SaveCharacterToDb()
        {
            if (isLoading) return;
            try
            {
                using var db = new DDInformationContext();
                var character = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                if (character == null) return;

                character.Name    = NameTextBox.Text;
                character.Hitpoints = (int)MaxHPUpDown.Value;
                character.CurHp   = (int)CurHPUpDown.Value;
                character.Exp     = (int)ExUpDown.Value;
                character.Notes   = NotesTextBox.Text;
                character.Speed   = (int)SpeedUpDown.Value;
                character.Gm      = (int)gmUpDown.Value;
                character.Mm      = (int)mmUpDown.Value;
                character.Sm      = (int)smUpDown.Value;
                character.Em      = (int)emUpDown.Value;
                character.Pm      = (int)pmUpDown.Value;
                character.Kd      = (int)KDUpDown.Value;
                character.TimeHitpoints = (int)CurDiceHPUpDown.Value;
                character.Possession    = OtherSkillsTextBox.Text;

                character.Characteristiks =
                    StrengthUpDown.Value     + ";" +
                    AgilityUpDown.Value      + ";" +
                    StaminaUpDown.Value      + ";" +
                    IntelligenceUpDown.Value + ";" +
                    WisdomUpDown.Value       + ";" +
                    CharismaUpDown.Value     + ";";

                // Спасброски
                string des = null;
                if (StrengthCheckBox.Checked)    des += "сил;";
                if (AgilityCheckBox.Checked)     des += "лов;";
                if (StaminaCheckBox.Checked)     des += "тел;";
                if (CharismaCheckBox.Checked)    des += "хар;";
                if (WisdomCheckBox.Checked)      des += "муд;";
                if (IntelligenceCheckBox.Checked) des += "инт;";
                character.Description = des;

                // Броски смерти
                int winCount = 0;
                if (WinCheck1.Checked) winCount++;
                if (WinCheck2.Checked) winCount++;
                if (WinCheck3.Checked) winCount++;
                character.SpasWin = winCount;

                int loseCount = 0;
                if (LoseCheck1.Checked) loseCount++;
                if (LoseCheck2.Checked) loseCount++;
                if (LoseCheck3.Checked) loseCount++;
                character.SpasLose = loseCount;

                // Навыки
                string possnew = null;
                foreach (var poss in GetAllSkillCheckBoxes())
                {
                    if (poss.CheckState == CheckState.Checked)
                        possnew += poss.Text.ToLower() + ";";
                    else if (poss.CheckState == CheckState.Indeterminate)
                        possnew += poss.Text.ToLower() + "+;";
                }
                character.PossesionNew = possnew;

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveCharacterToDb] {ex.Message}");
            }
        }
    }
}
