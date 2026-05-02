using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.Playwright;

namespace DnD_character_list
{
    public partial class Form4 : Form
    {
        public Form1 form1;
        public int DataIDCharacter;
        private bool isLoading = false;
        private GetOldStats oldStats = new GetOldStats();

        public Form4(int value)
        {
            DataIDCharacter = value;
            InitializeComponent();
            this.pictureBox2.MouseClick += pictureBox2_Click;
            Load_character();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3(form1);
            form3.Show();
            this.Hide();
        }

        private void Form4_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show(
                    "Вы уверены что хотите закрыть приложение?",
                    "Подтверждение выхода",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                    e.Cancel = true;
            }
        }

        private void NewSpellButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Ахуел?",
                "Подтверждение выход",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
        }

        private void DataBaseButton_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2(form1, DataIDCharacter);
            form2.Show();
            this.Hide();
        }

        void Load_character()
        {
            isLoading = true;
            using (var db = new DDInformationContext())
            {
                var character = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                if (character.Gm != null) gmUpDown.Value = character.Gm.Value;
                if (character.Mm != null) mmUpDown.Value = character.Mm.Value;
                if (character.Sm != null) smUpDown.Value = character.Sm.Value;
                if (character.Em != null) emUpDown.Value = character.Em.Value;
                if (character.Pm != null) pmUpDown.Value = character.Pm.Value;
                if (character.Name != null) NameTextBox.Text = character.Name;
                if (character.Hitpoints != null) MaxHPUpDown.Value = character.Hitpoints.Value;
                if (character.CurHp != null) CurHPUpDown.Value = character.CurHp.Value;
                if (character.Exp != null) ExUpDown.Value = character.Exp.Value;
                if (character.Notes != null) NotesTextBox.Text = character.Notes;
                if (character.Kd != null) KDUpDown.Value = character.Kd.Value;

                if (character.Description != null)
                {
                    foreach (string item in character.Description.Split(';'))
                    {
                        switch (item)
                        {
                            case "сил": StrengthCheckBox.Checked = true; break;
                            case "лов": AgilityCheckBox.Checked = true; break;
                            case "тел": StaminaCheckBox.Checked = true; break;
                            case "инт": IntelligenceCheckBox.Checked = true; break;
                            case "муд": WisdomCheckBox.Checked = true; break;
                            case "хар": CharismaCheckBox.Checked = true; break;
                        }
                    }
                }

                comboBackground.DataSource = db.Backgrounds.ToList();
                comboBackground.DisplayMember = "Name";
                comboBackground.ValueMember = "IdBackground";

                comboSpecies.DataSource = db.Species.ToList();
                comboSpecies.DisplayMember = "Name";
                comboSpecies.ValueMember = "IdSpecies";

                if (character.PossesionNew != null)
                {
                    foreach (string item in character.PossesionNew.Split(';'))
                    {
                        switch (item)
                        {
                            case "атлетика": AthleticsCheckBox.Checked = true; break;
                            case "акробатика": AcrobaticsCheckBox.Checked = true; break;
                            case "ловкость рук": DexterityCheckBox.Checked = true; break;
                            case "скрытность": StealthCheckBox.Checked = true; break;
                            case "анализ": DessectionCheckBox.Checked = true; break;
                            case "история": HistoryCheckBox.Checked = true; break;
                            case "магия": MagicCheckBox.Checked = true; break;
                            case "природа": NatureCheckBox.Checked = true; break;
                            case "религия": ReligionCheckBox.Checked = true; break;
                            case "восприятие": PerceptionCheckBox.Checked = true; break;
                            case "выживание": SurvivalCheckBox.Checked = true; break;
                            case "медицина": MedicineCheckBox.Checked = true; break;
                            case "проницание": DiscriminationCheckBox.Checked = true; break;
                            case "выступление": PerformanceCheckBox.Checked = true; break;
                            case "запугивание": IntimidationCheckBox.Checked = true; break;
                            case "обман": DeceptionCheckBox.Checked = true; break;
                            case "убеждение": PersuasionCheckBox.Checked = true; break;
                            case "атлетика+": AthleticsCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "акробатика+": AcrobaticsCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "ловкость рук+": DexterityCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "скрытность+": StealthCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "анализ+": DessectionCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "история+": HistoryCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "магия+": MagicCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "природа+": NatureCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "религия+": ReligionCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "восприятие+": PerceptionCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "выживание+": SurvivalCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "медицина+": MedicineCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "проницание+": DiscriminationCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "выступление+": PerformanceCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "запугивание+": IntimidationCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "обман+": DeceptionCheckBox.CheckState = CheckState.Indeterminate; break;
                            case "убеждение+": PersuasionCheckBox.CheckState = CheckState.Indeterminate; break;
                        }
                    }
                }

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

                if (character.Possession != null) OtherSkillsTextBox.Text = character.Possession;

                if (character.Characteristiks != null)
                {
                    var stats = character.Characteristiks.Split(';');
                    if (stats.Length >= 6)
                    {
                        StrengthUpDown.Value = decimal.Parse(stats[0]);
                        AgilityUpDown.Value = decimal.Parse(stats[1]);
                        StaminaUpDown.Value = decimal.Parse(stats[2]);
                        IntelligenceUpDown.Value = decimal.Parse(stats[3]);
                        WisdomUpDown.Value = decimal.Parse(stats[4]);
                        CharismaUpDown.Value = decimal.Parse(stats[5]);
                    }
                }

                comboSpecies.SelectedValue = character.IdSpecies;
                comboBackground.SelectedValue = character.IdBackground;

                if (character.Speed != null && character.Speed != 0)
                    SpeedUpDown.Value = character.Speed.Value;

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

                var bg = db.Backgrounds.FirstOrDefault(b => b.IdBackground == character.IdBackground);
                if (bg != null)
                {
                    BackgroundDescTextBox.Text = (bg.Description ?? "") + "\n \n" + bg.Invetary;
                    if (bg.ToolOwnership != null) ToolsTextBox.Text = bg.ToolOwnership;
                }

                isLoading = false;
            }
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            using (var db = new DDInformationContext())
            {
                var character = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                if (character != null)
                {
                    character.Name = NameTextBox.Text;
                    character.Hitpoints = (int)MaxHPUpDown.Value;
                    character.CurHp = (int)CurHPUpDown.Value;
                    character.Exp = (int)ExUpDown.Value;
                    character.Notes = NotesTextBox.Text;
                    character.Speed = (int)SpeedUpDown.Value;
                    character.Gm = (int)gmUpDown.Value;
                    character.Mm = (int)mmUpDown.Value;
                    character.Sm = (int)smUpDown.Value;
                    character.Em = (int)emUpDown.Value;
                    character.Pm = (int)pmUpDown.Value;

                    character.Characteristiks =
                        StrengthUpDown.Value + ";" +
                        AgilityUpDown.Value + ";" +
                        StaminaUpDown.Value + ";" +
                        IntelligenceUpDown.Value + ";" +
                        WisdomUpDown.Value + ";" +
                        CharismaUpDown.Value + ";";

                    string des = null;
                    if (StrengthCheckBox.Checked) des += "сил;";
                    if (AgilityCheckBox.Checked) des += "лов;";
                    if (StaminaCheckBox.Checked) des += "тел;";
                    if (CharismaCheckBox.Checked) des += "хар;";
                    if (WisdomCheckBox.Checked) des += "муд;";
                    if (IntelligenceCheckBox.Checked) des += "инт;";
                    character.Description = des;

                    character.Possession = OtherSkillsTextBox.Text;
                    character.Kd = (int)KDUpDown.Value;

                    int winspascount = 0;
                    if (WinCheck1.Checked) winspascount++;
                    if (WinCheck2.Checked) winspascount++;
                    if (WinCheck3.Checked) winspascount++;
                    character.SpasWin = winspascount;

                    int losespascount = 0;
                    if (LoseCheck1.Checked) losespascount++;
                    if (LoseCheck2.Checked) losespascount++;
                    if (LoseCheck3.Checked) losespascount++;
                    character.SpasLose = losespascount;

                    string possnew = null;
                    var possesions = new List<CheckBox> {
                        AthleticsCheckBox, AcrobaticsCheckBox, DexterityCheckBox, StealthCheckBox,
                        DessectionCheckBox, HistoryCheckBox, MagicCheckBox, NatureCheckBox,
                        ReligionCheckBox, PerceptionCheckBox, SurvivalCheckBox,
                        MedicineCheckBox, DiscriminationCheckBox, PerformanceCheckBox,
                        IntimidationCheckBox, DeceptionCheckBox, PersuasionCheckBox
                    };

                    foreach (var poss in possesions)
                    {
                        if (poss.CheckState == CheckState.Checked)
                            possnew += poss.Text.ToLower() + ";";
                        else if (poss.CheckState == CheckState.Indeterminate)
                            possnew += poss.Text.ToLower() + "+;";
                    }

                    character.PossesionNew = possnew;
                    db.SaveChanges();
                    MessageBox.Show("Сохранено!");
                }
                else
                {
                    MessageBox.Show("Персонаж не найден");
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var importerClasses = new ClassesImporter();
            await importerClasses.ImportClassAsync();
        }

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
                                    {
                                        character.Speed = walkSpeed;
                                        SpeedUpDown.Value = walkSpeed;
                                    }
                                    break;
                                case "пла":
                                case "лет":
                                    SpeedSkillTextBox.Text = "";
                                    break;
                            }
                        }
                    }

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
                                    {
                                        character.Speed = walkSpeed;
                                        SpeedUpDown.Value = walkSpeed;
                                    }
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

                    if (!string.IsNullOrEmpty(prevSpecies?.SpeciesChaTics))
                    {
                        foreach (var chart in prevSpecies.SpeciesChaTics.Split(';'))
                        {
                            if (string.IsNullOrEmpty(chart)) continue;
                            var chartick = chart.Split(':');
                            if (chartick.Length < 2) continue;
                            switch (chartick[0].Trim())
                            {
                                case "сил": if (int.TryParse(chartick[1], out int s)) StrengthUpDown.Value -= s; break;
                                case "лов": if (int.TryParse(chartick[1], out int a)) AgilityUpDown.Value -= a; break;
                                case "тел": if (int.TryParse(chartick[1], out int st)) StaminaUpDown.Value -= st; break;
                                case "инт": if (int.TryParse(chartick[1], out int i)) IntelligenceUpDown.Value -= i; break;
                                case "муд": if (int.TryParse(chartick[1], out int w)) WisdomUpDown.Value -= w; break;
                                case "хар": if (int.TryParse(chartick[1], out int c)) CharismaUpDown.Value -= c; break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(specie.SpeciesChaTics))
                    {
                        foreach (var chart in specie.SpeciesChaTics.Split(';'))
                        {
                            if (string.IsNullOrEmpty(chart)) continue;
                            var chartick = chart.Split(':');
                            switch (chartick[0].Trim())
                            {
                                case "сил": if (int.TryParse(chartick[1], out int s)) StrengthUpDown.Value += s; break;
                                case "лов": if (int.TryParse(chartick[1], out int a)) AgilityUpDown.Value += a; break;
                                case "тел": if (int.TryParse(chartick[1], out int st)) StaminaUpDown.Value += st; break;
                                case "инт": if (int.TryParse(chartick[1], out int i)) IntelligenceUpDown.Value += i; break;
                                case "муд": if (int.TryParse(chartick[1], out int w)) WisdomUpDown.Value += w; break;
                                case "хар": if (int.TryParse(chartick[1], out int c)) CharismaUpDown.Value += c; break;
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
                    charecter.IdBackground = backgroundID;

                    BackgroundDescTextBox.Text = (background.Description ?? "") + "\n \n" + background.Invetary;

                    var oldBg = oldStats.getoldbackground(DataIDCharacter);
                    int prev_gm = oldBg?.Gm ?? 0;

                    if (oldBg?.Possesion != null)
                    {
                        foreach (string item in oldBg.Possesion.Split(';'))
                        {
                            switch (item)
                            {
                                case "атлетика": AthleticsCheckBox.Checked = false; break;
                                case "акробатика": AcrobaticsCheckBox.Checked = false; break;
                                case "ловкость рук": DexterityCheckBox.Checked = false; break;
                                case "скрытность": StealthCheckBox.Checked = false; break;
                                case "анализ": DessectionCheckBox.Checked = false; break;
                                case "история": HistoryCheckBox.Checked = false; break;
                                case "магия": MagicCheckBox.Checked = false; break;
                                case "природа": NatureCheckBox.Checked = false; break;
                                case "религия": ReligionCheckBox.Checked = false; break;
                                case "восприятие": PerceptionCheckBox.Checked = false; break;
                                case "выживание": SurvivalCheckBox.Checked = false; break;
                                case "медицина": MedicineCheckBox.Checked = false; break;
                                case "проницание": DiscriminationCheckBox.Checked = false; break;
                                case "выступление": PerformanceCheckBox.Checked = false; break;
                                case "запугивание": IntimidationCheckBox.Checked = false; break;
                                case "обман": DeceptionCheckBox.Checked = false; break;
                                case "убеждение": PersuasionCheckBox.Checked = false; break;
                            }
                        }
                    }

                    if (background.Possesion != null)
                    {
                        foreach (string item in background.Possesion.Split(';'))
                        {
                            switch (item)
                            {
                                case "атлетика": AthleticsCheckBox.Checked = true; break;
                                case "акробатика": AcrobaticsCheckBox.Checked = true; break;
                                case "ловкость рук": DexterityCheckBox.Checked = true; break;
                                case "скрытность": StealthCheckBox.Checked = true; break;
                                case "анализ": DessectionCheckBox.Checked = true; break;
                                case "история": HistoryCheckBox.Checked = true; break;
                                case "магия": MagicCheckBox.Checked = true; break;
                                case "природа": NatureCheckBox.Checked = true; break;
                                case "религия": ReligionCheckBox.Checked = true; break;
                                case "восприятие": PerceptionCheckBox.Checked = true; break;
                                case "выживание": SurvivalCheckBox.Checked = true; break;
                                case "медицина": MedicineCheckBox.Checked = true; break;
                                case "проницание": DiscriminationCheckBox.Checked = true; break;
                                case "выступление": PerformanceCheckBox.Checked = true; break;
                                case "запугивание": IntimidationCheckBox.Checked = true; break;
                                case "обман": DeceptionCheckBox.Checked = true; break;
                                case "убеждение": PersuasionCheckBox.Checked = true; break;
                            }
                        }
                    }

                    string possnew = null;
                    var possesions = new List<CheckBox> {
                        AthleticsCheckBox, AcrobaticsCheckBox, DexterityCheckBox, StealthCheckBox,
                        DessectionCheckBox, HistoryCheckBox, MagicCheckBox, NatureCheckBox,
                        ReligionCheckBox, PerceptionCheckBox, SurvivalCheckBox,
                        MedicineCheckBox, DiscriminationCheckBox, PerformanceCheckBox,
                        IntimidationCheckBox, DeceptionCheckBox, PersuasionCheckBox
                    };

                    foreach (var poss in possesions)
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
