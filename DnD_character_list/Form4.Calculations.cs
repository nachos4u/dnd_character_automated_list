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

            static string Fmt(int m) => m >= 0 ? $"+{m}" : m.ToString();

            // ── Метки модификаторов характеристик ────────────────────────────────
            StrengthLable.Text     = Fmt(strMod);
            AgilityLable.Text      = Fmt(agiMod);
            StaminaLable.Text      = Fmt(staMod);
            IntelligenceLable.Text = Fmt(intMod);
            WisdomLable.Text       = Fmt(wisMod);
            CharismaLable.Text     = Fmt(chaMod);

            // ── Спасброски (мод + БМ если знание) ────────────────────────────────
            int strSpas = strMod + (StrengthCheckBox.CheckState    == CheckState.Checked ? bm : 0);
            int agiSpas = agiMod + (AgilityCheckBox.CheckState     == CheckState.Checked ? bm : 0);
            int staSpas = staMod + (StaminaCheckBox.CheckState     == CheckState.Checked ? bm : 0);
            int intSpas = intMod + (IntelligenceCheckBox.CheckState == CheckState.Checked ? bm : 0);
            int wisSpas = wisMod + (WisdomCheckBox.CheckState      == CheckState.Checked ? bm : 0);
            int chaSpas = chaMod + (CharismaCheckBox.CheckState    == CheckState.Checked ? bm : 0);

            StrSpas.Text = Fmt(strSpas);
            AgiSpas.Text = Fmt(agiSpas);
            StaSpas.Text = Fmt(staSpas);
            IntSpas.Text = Fmt(intSpas);
            WisSpas.Text = Fmt(wisSpas);
            ChaSpas.Text = Fmt(chaSpas);

            // ── Метки бонусов к навыкам ───────────────────────────────────────────
            // Checked = мод + БМ (умение),  Indeterminate = мод + 2*БМ (компетентность)
            int SkillBonus(CheckBox cb, int baseMod) =>
                cb.CheckState == CheckState.Indeterminate ? baseMod + bm * 2 :
                cb.CheckState == CheckState.Checked       ? baseMod + bm     : baseMod;

            AthleticsLable.Text      = Fmt(SkillBonus(AthleticsCheckBox,      strMod));
            AcrobaticsLable.Text     = Fmt(SkillBonus(AcrobaticsCheckBox,     agiMod));
            DexterityLable.Text      = Fmt(SkillBonus(DexterityCheckBox,      agiMod));
            StealthLable.Text        = Fmt(SkillBonus(StealthCheckBox,        agiMod));
            DissectionLable.Text     = Fmt(SkillBonus(DessectionCheckBox,     intMod));
            HistoryLable.Text        = Fmt(SkillBonus(HistoryCheckBox,        intMod));
            MagicLable.Text          = Fmt(SkillBonus(MagicCheckBox,          intMod));
            NatureLable.Text         = Fmt(SkillBonus(NatureCheckBox,         intMod));
            ReligionLable.Text       = Fmt(SkillBonus(ReligionCheckBox,       intMod));
            PerceptionLable.Text     = Fmt(SkillBonus(PerceptionCheckBox,     wisMod));
            SurvivalLable.Text       = Fmt(SkillBonus(SurvivalCheckBox,       wisMod));
            MedicineLable.Text       = Fmt(SkillBonus(MedicineCheckBox,       wisMod));
            DiscriminationLable.Text = Fmt(SkillBonus(DiscriminationCheckBox, wisMod));
            AnimalCareLable.Text     = Fmt(SkillBonus(AnimalCareCheckBox,     wisMod));
            PerformanceLable.Text    = Fmt(SkillBonus(PerformanceCheckBox,    chaMod));
            IntimidationLable.Text   = Fmt(SkillBonus(IntimidationCheckBox,   chaMod));
            DeceptionLable.Text      = Fmt(SkillBonus(DeceptionCheckBox,      chaMod));
            ConvictionLable.Text     = Fmt(SkillBonus(PersuasionCheckBox,     chaMod));

            // ── Пассивное восприятие ─────────────────────────────────────────────
            int percBonus = PerceptionCheckBox.CheckState == CheckState.Indeterminate ? bm * 2
                          : PerceptionCheckBox.Checked ? bm : 0;
            int passivePerc = Math.Clamp(10 + wisMod + percBonus,
                (int)PassivPerceptionUpDown.Minimum, (int)PassivPerceptionUpDown.Maximum);
            PassivPerceptionUpDown.Value = passivePerc;

            // ── Инициатива ───────────────────────────────────────────────────────
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

            // ── Физические показатели ─────────────────────────────────────────────
            ActiveWeightLable.Text    = $"{str * 15} фунтов";
            ActiveHightJumpLable.Text = $"{Math.Max(0, 3 + strMod)} фт";
            ActiveLegthJumpLable.Text = $"{str} фт";
        }

        // ─── Заклинательная характеристика по классу ──────────────────────────────
        private static string GetSpellcastingAbility(string className) =>
            GetBaseClassName(className) switch
            {
                "Бард"     or "Чародей" or "Паладин" or "Колдун"    => "Харизма",
                "Жрец"     or "Друид"   or "Следопыт" or "Монах"    => "Мудрость",
                "Волшебник" or "Изобретатель"                        => "Интеллект",
                _                                                    => ""
            };
    }
}
