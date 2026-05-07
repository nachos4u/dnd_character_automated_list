using System;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Пересчёт всех вычисляемых значений ───────────────────────────────────
        private void UpdateCalculatedValues()
        {
            if (isLoading) return;

            int str  = (int)StrengthUpDown.Value;
            int agi  = (int)AgilityUpDown.Value;
            int sta  = (int)StaminaUpDown.Value;
            int intl = (int)IntelligenceUpDown.Value;
            int wis  = (int)WisdomUpDown.Value;
            int cha  = (int)CharismaUpDown.Value;

            // Бонус мастерства (из BMLable, куда он записан в FillClassInfo)
            int bm = 2;
            if (!string.IsNullOrEmpty(BMLable.Text) &&
                int.TryParse(BMLable.Text.TrimStart('+'), out int parsedBm))
                bm = parsedBm;

            // Модификаторы: floor((score - 10) / 2)
            int strMod = (int)Math.Floor((str  - 10.0) / 2);
            int agiMod = (int)Math.Floor((agi  - 10.0) / 2);
            int staMod = (int)Math.Floor((sta  - 10.0) / 2);
            int intMod = (int)Math.Floor((intl - 10.0) / 2);
            int wisMod = (int)Math.Floor((wis  - 10.0) / 2);
            int chaMod = (int)Math.Floor((cha  - 10.0) / 2);
            int strSpas = (int)Math.Floor((str - 10.0) / 2);
            int agiSpas = (int)Math.Floor((agi - 10.0) / 2);
            int staSpas = (int)Math.Floor((sta - 10.0) / 2);
            int intSpas = (int)Math.Floor((intl - 10.0) / 2);
            int wisSpas = (int)Math.Floor((wis - 10.0) / 2);
            int chaSpas = (int)Math.Floor((cha - 10.0) / 2);

            if (StrengthCheckBox.CheckState == CheckState.Checked) strSpas += bm;
            if (AgilityCheckBox.CheckState == CheckState.Checked) agiSpas += bm;
            if (StaminaCheckBox.CheckState == CheckState.Checked) staSpas += bm;
            if (IntelligenceCheckBox.CheckState == CheckState.Checked) intSpas += bm;
            if (WisdomCheckBox.CheckState == CheckState.Checked) wisSpas += bm;
            if (CharismaCheckBox.CheckState == CheckState.Checked) chaSpas += bm;

            static string Fmt(int m) => m >= 0 ? $"+{m}" : m.ToString();

            StrengthLable.Text     = Fmt(strMod);
            AgilityLable.Text      = Fmt(agiMod);
            StaminaLable.Text      = Fmt(staMod);
            IntelligenceLable.Text = Fmt(intMod);
            WisdomLable.Text       = Fmt(wisMod);
            CharismaLable.Text     = Fmt(chaMod);

            StrSpas.Text = Fmt(strSpas);
            AgiSpas.Text = Fmt(agiSpas);
            StaSpas.Text = Fmt(staSpas);
            IntSpas.Text = Fmt(intSpas);
            WisSpas.Text = Fmt(wisSpas);
            ChaSpas.Text = Fmt(chaSpas);


            // ── Пассивное восприятие: 10 + Мод_Мудрости + БМ (если знание) ────────
            int percBonus = PerceptionCheckBox.CheckState == CheckState.Indeterminate ? bm * 2
                          : PerceptionCheckBox.Checked ? bm : 0;
            int passivePerc = Math.Clamp(10 + wisMod + percBonus,
                (int)PassivPerceptionUpDown.Minimum, (int)PassivPerceptionUpDown.Maximum);
            PassivPerceptionUpDown.Value = passivePerc;

            // ── Инициатива: модификатор Ловкости ──────────────────────────────────
            int initVal = Math.Clamp(agiMod,
                (int)InitiativaUpDown.Minimum, (int)InitiativaUpDown.Maximum);
            InitiativaUpDown.Value = initVal;

            // ── Заклинательная характеристика ─────────────────────────────────────
            string primaryClassName = ClassNameTextBox.Text.Split('\n')[0].Trim();
            string spellAbility = GetSpellcastingAbility(primaryClassName);
            int spellMod = spellAbility switch
            {
                "Харизма"   => chaMod,
                "Мудрость"  => wisMod,
                "Интеллект" => intMod,
                _           => 0
            };

            SpellCharaLable.Text             = string.IsNullOrEmpty(spellAbility) ? "—" : spellAbility;
            ActiveSpellModificationLable.Text = Fmt(spellMod);
            ActiveSpasLevelLable.Text         = (8 + bm + spellMod).ToString();
            ActiveAtakBonusLable.Text         = Fmt(bm + spellMod);

            // ── Физические показатели ──────────────────────────────────────────────
            // Грузоподъёмность = Сила × 6.75 кг (15 фунтов ≈ 6.8 кг)
            ActiveWeightLable.Text    = $"{str * 15} футов";
            // Прыжок в высоту = 3 + Мод_Силы фута
            ActiveHightJumpLable.Text = $"{Math.Max(0, 3 + strMod)} фт";
            // Прыжок в длину = Сила футов
            ActiveLegthJumpLable.Text = $"{str} фт";
        }

        // ─── Заклинательная характеристика по классу ──────────────────────────────
        private static string GetSpellcastingAbility(string className) =>
            GetBaseClassName(className) switch
            {
                "Бард"     or "Чародей" or "Паладин" or "Колдун" => "Харизма",
                "Жрец"     or "Друид"   or "Следопыт" or "Монах" => "Мудрость",
                "Волшебник" or "Изобретатель"                    => "Интеллект",
                _                                                => ""
            };
    }
}
