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
                {
                    e.Cancel = true;
                }
            }

        }


        private void NewSpellButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
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
                int speed = 0;
                if (character.Gm != null)
                {
                    gmUpDown.Value = character.Gm.Value;
                }
                if (character.Mm != null)
                {
                    mmUpDown.Value = character.Mm.Value;
                }
                if (character.Sm != null)
                {
                    smUpDown.Value = character.Sm.Value;
                }
                if (character.Em != null)
                {
                    emUpDown.Value = character.Em.Value;
                }
                if (character.Pm != null)
                {
                    pmUpDown.Value = character.Pm.Value;
                }
                if (character.Name != null)
                {
                    NameTextBox.Text = character.Name;
                }
                if (character.Hitpoints != null)
                {
                    MaxHPUpDown.Value = character.Hitpoints.Value;
                }
                if (character.CurHp != null)
                {
                    CurHPUpDown.Value += character.CurHp.Value;
                }
                if (character.Exp != null)
                {
                    ExUpDown.Value += character.Exp.Value;
                }
                if (character.Notes != null)
                {
                    NotesTextBox.Text = character.Notes.ToString();
                }
                if (character.Speed != null)
                {
                    speed = character.Speed.Value;
                }
                if (character.Kd != null)
                {
                    KDUpDown.Value = character.Kd.Value;
                }
                if (character.Description != null)
                {
                    var spas = character.Description.Split(";");

                    foreach (string items in spas)
                    {
                        switch (items)
                        {
                            case "сил":
                                StrengthCheckBox.Checked = true;
                                break;
                            case "лов":
                                AgilityCheckBox.Checked = true;
                                break;
                            case "тел":
                                StaminaCheckBox.Checked = true;
                                break;
                            case "инт":
                                IntelligenceCheckBox.Checked = true;
                                break;
                            case "муд":
                                WisdomCheckBox.Checked = true;
                                break;
                            case "хар":
                                CharismaCheckBox.Checked = true;
                                break;
                            default:
                                break;
                        }
                    }
                }

                using (var dbback = new DDInformationContext())
                {
                    comboBackground.DataSource = dbback.Backgrounds.ToList();
                    comboBackground.DisplayMember = "Name";
                    comboBackground.ValueMember = "IdBackground";
                }

                using (var dbspis = new DDInformationContext())
                {
                    comboSpecies.DataSource = dbspis.Species.ToList();
                    comboSpecies.DisplayMember = "Name";
                    comboSpecies.ValueMember = "IdSpecies";
                }

                if (character.PossesionNew != null)
                {
                    var spas = character.PossesionNew.Split(";");

                    foreach (string items in spas)
                    {
                        switch (items)
                        {
                            case "атлетика":
                                AthleticsCheckBox.Checked = true;
                                break;
                            case "акробатика":
                                AcrobaticsCheckBox.Checked = true;
                                break;
                            case "ловкость рук":
                                DexterityCheckBox.Checked = true;
                                break;
                            case "скрытность":
                                StealthCheckBox.Checked = true;
                                break;
                            case "анализ":
                                DessectionCheckBox.Checked = true;
                                break;
                            case "история":
                                HistoryCheckBox.Checked = true;
                                break;
                            case "магия":
                                MagicCheckBox.Checked = true;
                                break;
                            case "природа":
                                NatureCheckBox.Checked = true;
                                break;
                            case "религия":
                                ReligionCheckBox.Checked = true;
                                break;
                            case "восприятие":
                                PerceptionCheckBox.Checked = true;
                                break;
                            case "выживание":
                                SurvivalCheckBox.Checked = true;
                                break;
                            case "медицина":
                                MedicineCheckBox.Checked = true;
                                break;
                            case "проницание":
                                DiscriminationCheckBox.Checked = true;
                                break;
                            case "выступление":
                                PerformanceCheckBox.Checked = true;
                                break;
                            case "запугивание":
                                IntimidationCheckBox.Checked = true;
                                break;
                            case "обман":
                                DeceptionCheckBox.Checked = true;
                                break;
                            case "убеждение":
                                PersuasionCheckBox.Checked = true;
                                break;
                            case "атлетика+":
                                AthleticsCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "акробатика+":
                                AcrobaticsCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "ловкость рук+":
                                DexterityCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "скрытность+":
                                StealthCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "анализ+":
                                DessectionCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "история+":
                                HistoryCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "магия+":
                                MagicCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "природа+":
                                NatureCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "религия+":
                                ReligionCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "восприятие+":
                                PerceptionCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "выживание+":
                                SurvivalCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "медицина+":
                                MedicineCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "проницание+":
                                DiscriminationCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "выступление+":
                                PerformanceCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "запугивание+":
                                IntimidationCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "обман+":
                                DeceptionCheckBox.CheckState = CheckState.Indeterminate;
                                break;
                            case "убеждение+":
                                PersuasionCheckBox.CheckState = CheckState.Indeterminate;
                                break;

                            default:
                                break;
                        }
                    }
                }

                if (character.SpasWin != null)
                {
                    switch (character.SpasWin)
                    {
                        case 1:
                            WinCheck1.Checked = true; break;
                        case 2:
                            WinCheck1.Checked = true;
                            WinCheck2.Checked = true; break;
                        case 3:
                            WinCheck2.Checked = true;
                            WinCheck1.Checked = true;
                            WinCheck3.Checked = true; break;

                        default:
                            break;
                    }
                }
                if (character.SpasLose != null)
                {
                    switch (character.SpasLose)
                    {
                        case 1:
                            LoseCheck1.Checked = true; break;
                        case 2:
                            LoseCheck1.Checked = true;
                            LoseCheck2.Checked = true; break;
                        case 3:
                            LoseCheck2.Checked = true;
                            LoseCheck1.Checked = true;
                            LoseCheck3.Checked = true; break;

                        default:
                            break;
                    }
                }

                if (character.Possession != null)
                {
                    OtherSkillsTextBox.Text = character.Possession;
                }

                if (character.Characteristiks != null)
                {
                    var stats = character.Characteristiks.Split(';');

                    if (stats.Length == 7)
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

                if (speed != 0)
                {
                    SpeedUpDown.Value = speed;
                }

                comboBackground.SelectedValue = character.IdBackground;
                isLoading = false;
            }


        }


        private void ReloadButton_Click(object sender, EventArgs e)
        {
            using (var db = new DDInformationContext())
            {
                var character = db.Characters
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

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

                    string charist = StrengthUpDown.Value.ToString() + ";" +
                        AgilityUpDown.Value.ToString() + ";" +
                        StaminaUpDown.Value.ToString() + ";" +
                        IntelligenceUpDown.Value.ToString() + ";" +
                        WisdomUpDown.Value.ToString() + ";" +
                        CharismaUpDown.Value.ToString() + ";";
                    character.Characteristiks = charist;

                    string des = null;
                    if (StrengthCheckBox.Checked)
                    {
                        des += "сил;";
                    }
                    if (AgilityCheckBox.Checked)
                    {
                        des += "лов;";
                    }
                    if (StaminaCheckBox.Checked)
                    {
                        des += "тел;";
                    }
                    if (CharismaCheckBox.Checked)
                    {
                        des += "хар;";
                    }
                    if (WisdomCheckBox.Checked)
                    {
                        des += "муд;";
                    }
                    if (IntelligenceCheckBox.Checked)
                    {
                        des += "инт;";
                    }
                    character.Description = des;

                    character.Possession = OtherSkillsTextBox.Text;
                    character.Kd = (int)KDUpDown.Value;
                    int winspascount = 0;
                    if (WinCheck1.Checked)
                    {
                        winspascount++;
                    }
                    if (WinCheck2.Checked)
                    {
                        winspascount++;
                    }
                    if (WinCheck3.Checked)
                    {
                        winspascount++;
                    }
                    character.SpasWin = winspascount;

                    int losespascount = 0;
                    if (LoseCheck1.Checked)
                    {
                        losespascount++;
                    }
                    if (LoseCheck2.Checked)
                    {
                        losespascount++;
                    }
                    if (LoseCheck3.Checked)
                    {
                        losespascount++;
                    }
                    character.SpasLose = losespascount;

                    string possnew = null;
                    var possesions = new List<CheckBox> { AthleticsCheckBox,
                    AcrobaticsCheckBox, DexterityCheckBox, StealthCheckBox,
                    DessectionCheckBox, HistoryCheckBox, MagicCheckBox, NatureCheckBox,
                    ReligionCheckBox, PerceptionCheckBox, SurvivalCheckBox,
                    MedicineCheckBox, DiscriminationCheckBox, PerformanceCheckBox,
                    IntimidationCheckBox, DeceptionCheckBox, PersuasionCheckBox};

                    foreach (var poss in possesions)
                    {
                        if (poss.CheckState == CheckState.Checked)
                        {
                            possnew += poss.Text.ToLower() + ";";
                        }
                        if (poss.CheckState == CheckState.Indeterminate)
                        {
                            possnew += poss.Text.ToLower() + "+;";
                        }
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
            //var importerSpells = new SpellsImporter();
            //var importerSpecies = new SpeciesImporter();
            //var importerBackground = new BackgroundImporter();
            var importerClasses = new ClassesImporter();
            //await importerSpecies.ImportRacesAsync();
            //await importerBackground.ImportBackgroundAsync();
            //await importerSpells.ImportSpellAsync();
            await importerClasses.ImportClassAsync();
        }



        private void comboSpecies_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Check if there's a valid selection

            if (isLoading) return;
            var selectedSpecies = comboSpecies.SelectedItem as Species;
            if (selectedSpecies == null) return;

            int specieId = selectedSpecies.IdSpecies;


            try
            {
                using (var db = new DDInformationContext())
                {
                    // Execute query immediately with FirstOrDefault
                    var specie = db.Species
                        .Where(s => s.IdSpecies == specieId)
                        .Select(s => new
                        {
                            s.Speed,
                            s.SpeciesChaTics,
                            s.SpeciesSkills,
                            s.IdSpecies
                        })
                        .FirstOrDefault(); // This actually executes the query

                    if (specie == null) return;

                    var character = db.Characters
                        .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                    if (character == null) return;

                    character.IdSpecies = specieId;

                    // Fix 1: Use actual property values, not ToString() on query
                    SpecieSkillsTextBox.Text = specie.SpeciesSkills ?? "";

                    var prevSpecies = oldStats.getoldspecie(DataIDCharacter);

                    if (!string.IsNullOrEmpty(prevSpecies.Speed))
                    {
                        var speeds = prevSpecies.Speed.Split(';');
                        foreach (var speed in speeds)
                        {
                            if (string.IsNullOrEmpty(speed)) continue;

                            var aids = speed.Split(':');
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
                                    SpeedSkillTextBox.Text = "";
                                    break;
                                case "лет":
                                    SpeedSkillTextBox.Text = "";
                                    break;
                            }
                        }
                    }

                    // Fix 2: Check for null before splitting
                    if (!string.IsNullOrEmpty(specie.Speed))
                    {
                        var speeds = specie.Speed.Split(';');
                        foreach (var speed in speeds)
                        {
                            if (string.IsNullOrEmpty(speed)) continue;

                            var aids = speed.Split(':');
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

                        if (!string.IsNullOrEmpty(prevSpecies.SpeciesChaTics))
                    {
                        var charticks = prevSpecies.SpeciesChaTics.Split(';');
                        foreach (var chart in charticks)
                        {
                            if (string.IsNullOrEmpty(chart)) continue;

                            var chartick = chart.Split(':');
                            if (chartick.Length < 2) continue;

                            switch (chartick[0].Trim())
                            {
                                case "сил":
                                    if (int.TryParse(chartick[1], out int strBonus))
                                        StrengthUpDown.Value -= strBonus;
                                    break;
                                case "лов":
                                    if (int.TryParse(chartick[1], out int agiBonus))
                                        AgilityUpDown.Value -= agiBonus;
                                    break;
                                case "тел":
                                    if (int.TryParse(chartick[1], out int staBonus))
                                        StaminaUpDown.Value -= staBonus;
                                    break;
                                case "инт":
                                    if (int.TryParse(chartick[1], out int intBonus))
                                        IntelligenceUpDown.Value -= intBonus;
                                    break;
                                case "муд":
                                    if (int.TryParse(chartick[1], out int wisBonus))
                                        WisdomUpDown.Value -= wisBonus;
                                    break;
                                case "хар":
                                    if (int.TryParse(chartick[1], out int chaBonus))
                                        CharismaUpDown.Value -= chaBonus;
                                    break;
                            }
                        }
                    }

                    // Fix 3: Handle characteristics properly
                    if (!string.IsNullOrEmpty(specie.SpeciesChaTics))
                    {
                        var charticks = specie.SpeciesChaTics.Split(';');
                        foreach (var chart in charticks)
                        {
                            if (string.IsNullOrEmpty(chart)) continue;

                            var chartick = chart.Split(':');

                            switch (chartick[0].Trim())
                            {
                                case "сил":
                                    if (int.TryParse(chartick[1], out int strBonus))
                                        StrengthUpDown.Value += strBonus;
                                    break;
                                case "лов":
                                    if (int.TryParse(chartick[1], out int agiBonus))
                                        AgilityUpDown.Value += agiBonus;
                                    break;
                                case "тел":
                                    if (int.TryParse(chartick[1], out int staBonus))
                                        StaminaUpDown.Value += staBonus;
                                    break;
                                case "инт":
                                    if (int.TryParse(chartick[1], out int intBonus))
                                        IntelligenceUpDown.Value += intBonus;
                                    break;
                                case "муд":
                                    if (int.TryParse(chartick[1], out int wisBonus))
                                        WisdomUpDown.Value += wisBonus;
                                    break;
                                case "хар":
                                    if (int.TryParse(chartick[1], out int chaBonus))
                                        CharismaUpDown.Value += chaBonus;
                                    break;
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
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void comboBackground_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;
            var selectedBackground = comboBackground.SelectedItem as Background;
            int backgroundID = selectedBackground.IdBackground;
            try
            {
                using (var db = new DDInformationContext())
                {

                    var background = db.Backgrounds.Where(b => b.IdBackground == backgroundID)
                        .Select(b => new
                        {
                            b.Name,
                            b.Possesion,
                            b.Invetary,
                            b.Gm,
                            b.Description,
                            b.ToolOwnership
                        }).FirstOrDefault();
                    var charecter = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                    charecter.IdBackground = backgroundID;

                    BackgroundDescTextBox.Text = background.Description ?? "";

                    if (oldStats.getoldbackground(DataIDCharacter).Possesion != null)
                    {
                        var spas = oldStats.getoldbackground(DataIDCharacter).Possesion.Split(";");

                        foreach (string items in spas)
                        {
                            switch (items)
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
                        var spas = background.Possesion.Split(";");

                        foreach (string items in spas)
                        {
                            switch (items)
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

                    var possesions = new List<CheckBox> { AthleticsCheckBox,
                    AcrobaticsCheckBox, DexterityCheckBox, StealthCheckBox,
                    DessectionCheckBox, HistoryCheckBox, MagicCheckBox, NatureCheckBox,
                    ReligionCheckBox, PerceptionCheckBox, SurvivalCheckBox,
                    MedicineCheckBox, DiscriminationCheckBox, PerformanceCheckBox,
                    IntimidationCheckBox, DeceptionCheckBox, PersuasionCheckBox};

                    foreach (var poss in possesions) {
                        if (poss.CheckState == CheckState.Checked)
                        {
                            possnew += poss.Text.ToLower() + ";";
                        }
                        if (poss.CheckState == CheckState.Indeterminate) {
                            possnew += poss.Text.ToLower() + "+;";
                        }
                    }

                    charecter.PossesionNew = possnew;


                    BackgroundDescTextBox.Text += "\n \n" + background.Invetary;
                    int prev_gm = 0;
                    if (oldStats.getoldbackground(DataIDCharacter).Gm != null)
                    {
                        prev_gm = (int)oldStats.getoldbackground(DataIDCharacter).Gm;
                    }
                    if ((int)gmUpDown.Value - prev_gm >= 0)
                        {
                        gmUpDown.Value -= prev_gm;

                        charecter.Gm -= prev_gm;
                        if (background.Gm != null)
                        {
                            gmUpDown.Value += (int)background.Gm;
                            charecter.Gm += (int)background.Gm;
                        }
                    }
                    else
                    {
                        gmUpDown.Value = (int)(background.Gm ?? 0);
                        charecter.Gm = (int)(background.Gm ?? 0);
                    }
                    charecter.Gm = (int)gmUpDown.Value;


                    db.SaveChanges();
                    if (background.ToolOwnership != null)
                    {
                        ToolsTextBox.Text = background.ToolOwnership;
                    }
                    

                    
                }
                
            }
            catch (Exception ex) {
                Console.WriteLine($"Ошибка {ex.Message}");
            }
        }


    }
}
