using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4 : Form
    {
        private Form1 _form1;
        public int DataIDCharacter;
        private bool isLoading = false;
        private GetOldStats oldStats = new GetOldStats();

        // Анимация выбора навыков
        private readonly System.Windows.Forms.Timer _animTimer = new System.Windows.Forms.Timer { Interval = 500 };
        private List<CheckBox> _pendingCheckBoxes = new List<CheckBox>();
        private int _pendingSkillCount = 0;
        private bool _animState = false;

        // Вторая страница для класса
        private TextBox FullClassTextBox2 = null;

        // Таблица ячеек мультикласса (D&D 5e PHB, эффективный уровень заклинателя 1–20)
        private static readonly int[][] _multiclassSlotTable =
        {
            new[]{2,0,0,0,0,0,0,0,0}, new[]{3,0,0,0,0,0,0,0,0}, new[]{4,2,0,0,0,0,0,0,0},
            new[]{4,3,0,0,0,0,0,0,0}, new[]{4,3,2,0,0,0,0,0,0}, new[]{4,3,3,0,0,0,0,0,0},
            new[]{4,3,3,1,0,0,0,0,0}, new[]{4,3,3,2,0,0,0,0,0}, new[]{4,3,3,3,1,0,0,0,0},
            new[]{4,3,3,3,2,0,0,0,0}, new[]{4,3,3,3,2,1,0,0,0}, new[]{4,3,3,3,2,1,0,0,0},
            new[]{4,3,3,3,2,1,1,0,0}, new[]{4,3,3,3,2,1,1,0,0}, new[]{4,3,3,3,2,1,1,1,0},
            new[]{4,3,3,3,2,1,1,1,0}, new[]{4,3,3,3,2,1,1,1,1}, new[]{4,3,3,3,3,1,1,1,1},
            new[]{4,3,3,3,3,2,1,1,1}, new[]{4,3,3,3,3,2,2,1,1},
        };

        public Form4(int value, Form1 form1 = null)
        {
            DataIDCharacter = value;
            _form1 = form1;
            InitializeComponent();

            // Fix 6: сдвигаем все контролы с Y >= 3778 вниз на 1794px, чтобы вставить вторую страницу класса
            const int shiftY = 1794;
            const int splitY = 3778;
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.Location.Y >= splitY)
                    ctrl.Location = new Point(ctrl.Location.X, ctrl.Location.Y + shiftY);
            }

            // Добавляем вторую страницу (pictureBox + label + textbox)
            // ВАЖНО: сначала добавляем label и textbox, потом pictureBox.SendToBack(),
            // иначе pictureBox окажется поверх них (Controls.Add добавляет в конец = сзади).
            var newPb = new PictureBox();
            newPb.Image = Properties.Resources.SecondList;
            newPb.Location = new Point(45, 3778);
            newPb.Size = new Size(1240, 1754);
            newPb.TabStop = false;

            var newLbl = new Label();
            newLbl.AutoSize = true;
            newLbl.Location = new Point(525, 3869);
            newLbl.Text = "Полная информация о классе (продолжение)";

            FullClassTextBox2 = new TextBox();
            FullClassTextBox2.Location = new Point(148, 3920);
            FullClassTextBox2.Multiline = true;
            FullClassTextBox2.ScrollBars = ScrollBars.Vertical;
            FullClassTextBox2.Size = new Size(1036, 1528);

            this.Controls.Add(newPb);
            this.Controls.Add(newLbl);
            this.Controls.Add(FullClassTextBox2);
            newPb.SendToBack(); // вызываем ПОСЛЕ добавления всех трёх — уходит за label и textbox

            this.pictureBox2.MouseClick += pictureBox2_Click;
            _animTimer.Tick += AnimTimer_Tick;
            Load_character();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3(_form1);
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
            Form2 form2 = new Form2(DataIDCharacter);
            form2.Show();
            this.Hide();
        }

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
                            case "уход за животными": AnimalCareCheckBox.Checked = true; break;
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
                            case "уход за животными+": AnimalCareCheckBox.Checked = true; break;
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

                UpdateLevelButton(character);

                ClassNameTextBox.Text = string.Join("\n", character.Levels
                    .Select(l => l.IdClassNavigation?.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct());

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
                    BackgroundDescTextBox.Text = (bg.Description ?? "") + "\n \n" + bg.Invetary;

                CurDiceHPUpDown.Value = (int)character.TimeHitpoints;

                FillClassInfo(character);

                if (character.SkillsPending && !string.IsNullOrEmpty(character.PendingSkillChoices))
                {
                    var skillList = character.PendingSkillChoices.Split(';').ToList();
                    StartSkillAnimation(skillList, character.PendingSkillCount ?? 2);
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
                        MedicineCheckBox, DiscriminationCheckBox, AnimalCareCheckBox, PerformanceCheckBox,
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
                    character.TimeHitpoints = (int)CurDiceHPUpDown.Value;
                    db.SaveChanges();
                    MessageBox.Show("Сохранено!");
                }
                else
                {
                    MessageBox.Show("Персонаж не найден");
                }
            }
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
                    if (charecter == null) return;

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
                                case "проницательность": DiscriminationCheckBox.Checked = false; break;
                                case "уход за животными": AnimalCareCheckBox.Checked = false; break;
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
                                case "проницательность": DiscriminationCheckBox.Checked = true; break;
                                case "уход за животными": AnimalCareCheckBox.Checked = true; break;
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
                        MedicineCheckBox, DiscriminationCheckBox, AnimalCareCheckBox, PerformanceCheckBox,
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

        private void LevelButton_Click(object sender, EventArgs e)
        {
            var existingClassIds = new List<int>();
            var existingLevels = new List<int>();

            using (var db = new DDInformationContext())
            {
                var character = db.Characters
                    .Include(c => c.Levels)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                if (character != null)
                {
                    foreach (var cl in character.Levels.GroupBy(l => l.IdClass))
                    {
                        existingClassIds.Add(cl.Key);
                        existingLevels.Add(cl.Max(l => l.Level1));
                    }
                }
            }

            using (Form5 modalForm = new Form5(existingClassIds, existingLevels))
            {
                DialogResult result = modalForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    using (var db = new DDInformationContext())
                    {
                        var character = db.Characters
                            .Include(c => c.Levels).ThenInclude(l => l.IdClassNavigation)
                            .Include(c => c.IdBackgroundNavigation)
                            .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                        var selectedClassIds = modalForm.SelectedClassIds;
                        var selectedLevels = modalForm.SelectedLevels;

                        foreach (var removedClassId in existingClassIds.Except(selectedClassIds))
                            SetClassLevel(character, removedClassId, 0, db);

                        for (int i = 0; i < selectedClassIds.Count; i++)
                            SetClassLevel(character, selectedClassIds[i], selectedLevels[i], db);

                        UpdateLevelButton(character);
                        if (isLoading) return;
                        character = db.Characters
                            .Include(c => c.Levels).ThenInclude(l => l.IdClassNavigation)
                            .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                        var chosenClasesIds = character.Levels
                                .Select(l => l.IdClassNavigation)
                                .DistinctBy(c => c.IdClass)
                                .ToList();
                        List<List<Level>> chosenLevels = new List<List<Level>>();
                        foreach (var cls in chosenClasesIds)
                        {
                            chosenLevels.Add(character.Levels
                                .Where(l => l.IdClass == cls.IdClass)
                                .ToList());
                        }

                        // Fix 5: если классов нет — очищаем и выходим
                        if (!chosenClasesIds.Any())
                        {
                            ClassNameTextBox.Text = "";
                            FillClassInfo(character);
                            return;
                        }

                        // Fix 1: имя класса с подклассом (полное cls.Name)
                        ClassNameTextBox.Text = string.Join("\n", chosenClasesIds.Select(cls => cls.Name));

                        // Парсим Possession первого класса
                        string classSpas = "";
                        List<string> classPossOld = new List<string>();
                        List<string> classPoss = new List<string>();
                        int possNumber = 2;
                        var firstClassObj = db.Classes.FirstOrDefault(c => c.IdClass == chosenClasesIds[0].IdClass);
                        if (firstClassObj != null)
                        {
                            foreach (var pos in firstClassObj.Possession.Split('\n'))
                            {
                                if (string.IsNullOrEmpty(pos)) continue;
                                var po = pos.Split(": ", 3);
                                if (po.Length < 2) continue;
                                switch (po[0])
                                {
                                    case "Спаcброски": classSpas = po[1]; break;
                                    case "Навыки":
                                        if (po[1].Contains("4")) possNumber = 4;
                                        else if (po[1].Contains("3")) possNumber = 3;
                                        else possNumber = 2;
                                        if (po.Length >= 3)
                                        {
                                            var map = BuildSkillMap();
                                            Background back;
                                            var ch = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                                            back = db.Backgrounds.FirstOrDefault(b => b.IdBackground == ch.IdBackground);
                                            var backPoss = back?.Possesion.Split(";");
                                            classPossOld = (po[2].Split(", ").ToList());
                                            foreach (var cls in classPossOld) {
                                                if (!backPoss.Contains(cls)) {
                                                    classPoss.Add(cls);
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }

                        // Fix 4: очищаем старые спасброски перед применением новых
                        StrengthCheckBox.Checked = false;
                        AgilityCheckBox.Checked = false;
                        StaminaCheckBox.Checked = false;
                        IntelligenceCheckBox.Checked = false;
                        WisdomCheckBox.Checked = false;
                        CharismaCheckBox.Checked = false;

                        // Спасброски
                        foreach (var spas in classSpas.Split(", "))
                        {
                            switch (spas.Trim())
                            {
                                case "Сила": StrengthCheckBox.Checked = true; break;
                                case "Ловкость": AgilityCheckBox.Checked = true; break;
                                case "Телосложение": StaminaCheckBox.Checked = true; break;
                                case "Интеллект": IntelligenceCheckBox.Checked = true; break;
                                case "Мудрость": WisdomCheckBox.Checked = true; break;
                                case "Харизма": CharismaCheckBox.Checked = true; break;
                            }
                        }

                        // Смена первичного класса → MessageBox + анимация
                        int? oldPrimaryId = existingClassIds.Count > 0 ? existingClassIds[0] : (int?)null;
                        int? newPrimaryId = selectedClassIds.Count > 0 ? selectedClassIds[0] : (int?)null;
                        if (newPrimaryId.HasValue && newPrimaryId != oldPrimaryId && classPoss.Any())
                        {
                            MessageBox.Show(
                                $"Выберите {possNumber} навыка из следующих:\n{string.Join(", ", classPoss)}",
                                "Выбор навыков класса", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            character.PrimaryClassId = newPrimaryId;
                            character.SkillsPending = true;
                            character.PendingSkillChoices = string.Join(";", classPoss);
                            character.PendingSkillCount = possNumber;
                            db.SaveChanges();

                            StartSkillAnimation(classPoss, possNumber);
                        }

                        // Заполняем все поля класса
                        FillClassInfo(character);

                    }
                }
            }
        }

        private void UpdateLevelButton(Character character)
        {
            if (character?.Levels == null || !character.Levels.Any())
            {
                LevelButton.Text = "";
                return;
            }

            int totalLevel = character.Levels
                .GroupBy(l => l.IdClass)
                .Sum(g => g.Max(l => l.Level1));
            DiceHPLable.Text = totalLevel.ToString();

            LevelButton.Text = totalLevel.ToString();
        }

        // ─── Вспомогательные методы ────────────────────────────────────────────────

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

        private static int GetCasterLevel(string className, int classLevel)
        {
            return GetBaseClassName(className) switch
            {
                "Бард" or "Жрец" or "Друид" or "Чародей" or "Волшебник" => classLevel,
                "Паладин" or "Следопыт" => classLevel / 2,
                "Плут" or "Воин" => classLevel / 3,
                _ => 0
            };
        }

        private static int GetDivineChannelCount(string className, int classLevel)
        {
            return GetBaseClassName(className) switch
            {
                "Жрец" when classLevel >= 18 => 3,
                "Жрец" when classLevel >= 6  => 2,
                "Жрец" when classLevel >= 2  => 1,
                "Паладин" when classLevel >= 3 => 1,
                _ => 0
            };
        }

        private static string GetMulticlassEquipment(string className)
        {
            return GetBaseClassName(className) switch
            {
                "Бард"      => "Лёгкие доспехи, 1 навык, 1 муз. инструмент",
                "Жрец"      => "Лёгкие и средние доспехи, щиты",
                "Друид"     => "Лёгкие и средние доспехи, щиты",
                "Воин"      => "Лёгкие и средние доспехи, щиты, простое и воинское оружие",
                "Следопыт"  => "Лёгкие и средние доспехи, щиты, простое и воинское оружие",
                "Паладин"   => "Лёгкие и средние доспехи, щиты, простое и воинское оружие",
                "Плут"      => "Лёгкие доспехи, 1 навык, воровские инструменты",
                "Монах"     => "Простое оружие, короткие мечи",
                "Варвар"    => "Щиты, воинское оружие",
                "Колдун"    => "Лёгкие доспехи, простое оружие",
                _           => "—"
            };
        }

        // ─── Заполнение полей класса ───────────────────────────────────────────────

        private void FillClassInfo(Character character)
        {
            var cellLabels = new Label[] {
                CellLevel1Lable, CellLevel2Lable, CellLevel3Lable,
                CellLevel4Lable, CellLevel5Lable, CellLevel6Lable,
                CellLevel7Lable, CellLevel8Lable, CellLevel9Lable
            };

            // Фоновые инструменты
            string bgTools = character.IdBackgroundNavigation?.ToolOwnership ?? "";
            var toolsSb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(bgTools))
                toolsSb.AppendLine(bgTools.TrimEnd());

            if (!character.Levels.Any())
            {
                ToolsTextBox.Text = toolsSb.ToString();
                FullClassTextBox.Text = "";
                if (FullClassTextBox2 != null) FullClassTextBox2.Text = "";
                ClassSkillsShortTextBox.Text = "";
                BMLable.Text = "";
                foreach (var lbl in cellLabels) lbl.Text = "";
                return;
            }

            // Группируем по классу
            var classGroups = character.Levels
                .GroupBy(l => l.IdClass)
                .Select(g => new
                {
                    Class   = g.First().IdClassNavigation,
                    MaxLvl  = g.Max(l => l.Level1),
                    Levels  = g.OrderBy(l => l.Level1).ToList()
                }).ToList();

            bool isMulti = classGroups.Count > 1;
            int totalLvl = classGroups.Sum(g => g.MaxLvl);

            // Fix 3: инструменты/доспехи/оружие первого класса (всегда, даже в мультиклассе)
            var firstCls = classGroups[0].Class;
            if (!string.IsNullOrEmpty(firstCls.Possession))
            {
                var classEquipSb = new System.Text.StringBuilder();
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

            // ── БМ ──────────────────────────────────────────────────────────────────
            int bm = 2 + (totalLvl - 1) / 4;
            BMLable.Text = $"+{bm}";

            // Fix 6: FullClassTextBox + FullClassTextBox2 (полные Skills всех уровней)
            // Один класс → уровни 1-10 в box1, 11-20 в box2
            // Несколько классов → первая половина в box1, вторая в box2
            {
                var sb1 = new System.Text.StringBuilder();
                var sb2 = new System.Text.StringBuilder();
                bool singleClass = classGroups.Count == 1;
                int splitClassCount = (classGroups.Count + 1) / 2;

                for (int ci = 0; ci < classGroups.Count; ci++)
                {
                    var cg = classGroups[ci];
                    if (singleClass)
                    {
                        // Для одного класса: уровни 1-10 → box1, 11-20 → box2
                        sb1.AppendLine($"=== {cg.Class.Name} (уровень {cg.MaxLvl}) — уровни 1–10 ===");
                        sb2.AppendLine($"=== {cg.Class.Name} (уровень {cg.MaxLvl}) — уровни 11–20 ===");
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
                        // Для нескольких классов: первая половина → box1, вторая → box2
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

            // ── ClassSkillsShortTextBox ─────────────────────────────────────────────
            var featureNames = new Dictionary<string, string>
            {
                ["СА"]  = "Скрытая атака",      ["ЯР"]  = "Ярость",
                ["УЯР"] = "Урон ярости",         ["БИ"]  = "Боевые искусства",
                ["ОЦ"]  = "Очки ци",             ["СБД"] = "Скорость без доспехов",
                ["ЕЧ"]  = "Единицы чародейства", ["ВЗ"]  = "Воззвания",
                ["КЗ"]  = "Заговоры",            ["ИЗ"]  = "Известные заклинания",
                ["ЯЧ"]  = "Ячейки (чернокн.)",  ["УЯ"]  = "Уровень ячеек (чернокн.)",
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
                        featDict[kv.Key] = kv.Value; // перезаписываем — берём последний (максимальный) уровень
                }
            }

            var shortSb = new System.Text.StringBuilder();
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

            // ── Ячейки заклинаний ────────────────────────────────────────────────────
            string warlockSlots = null, warlockSlotLvl = null;

            if (!isMulti)
            {
                // Fix 2: каждая ячейка показывает кружочки (○) по числу слотов
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

                // Снаряжение мультикласса (ограниченное для доп. классов)
                var mcEquipSb = new System.Text.StringBuilder();
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

        // ─── Анимация навыков ──────────────────────────────────────────────────────

        private Dictionary<string, CheckBox> BuildSkillMap() =>
            new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase)
            {
                ["атлетика"]           = AthleticsCheckBox,
                ["акробатика"]         = AcrobaticsCheckBox,
                ["ловкость рук"]       = DexterityCheckBox,
                ["скрытность"]         = StealthCheckBox,
                ["анализ"]             = DessectionCheckBox,
                ["история"]            = HistoryCheckBox,
                ["магия"]              = MagicCheckBox,
                ["природа"]            = NatureCheckBox,
                ["религия"]            = ReligionCheckBox,
                ["восприятие"]         = PerceptionCheckBox,
                ["выживание"]          = SurvivalCheckBox,
                ["медицина"]           = MedicineCheckBox,
                ["проницательность"]   = DiscriminationCheckBox,
                ["проницание"]         = DiscriminationCheckBox,
                ["уход за животными"]  = AnimalCareCheckBox,
                ["выступление"]        = PerformanceCheckBox,
                ["запугивание"]        = IntimidationCheckBox,
                ["обман"]              = DeceptionCheckBox,
                ["убеждение"]          = PersuasionCheckBox,
            };

        private void StartSkillAnimation(List<string> skillNames, int requiredCount)
        {
            StopSkillAnimationOnly();
            var map = BuildSkillMap();
            Background back;
            using (var db = new DDInformationContext())
            {
                var ch = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                back = db.Backgrounds.FirstOrDefault(b => b.IdBackground == ch.IdBackground);
            }
            var backPoss = back?.Possesion.Split(";");
            _pendingCheckBoxes = skillNames
                .Select(s => map.TryGetValue(s.Trim(), out var cb) ? cb : null)
                .Where(cb => cb != null).ToList();
            _pendingSkillCount = requiredCount;
            foreach (var cb in _pendingCheckBoxes)
                cb.CheckedChanged += CheckSkillProgress;
            _animTimer.Start();
        }

        private void StopSkillAnimationOnly()
        {
            _animTimer.Stop();
            foreach (var cb in _pendingCheckBoxes)
            {
                cb.BackColor = SystemColors.Control;
                cb.CheckedChanged -= CheckSkillProgress;
            }
            _pendingCheckBoxes.Clear();
        }

        private void StopSkillAnimation()
        {
            StopSkillAnimationOnly();
            using (var db = new DDInformationContext())
            {
                var ch = db.Characters.Find(DataIDCharacter);
                if (ch != null) { ch.SkillsPending = false; db.SaveChanges(); }
            }
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            _animState = !_animState;
            foreach (var cb in _pendingCheckBoxes)
                cb.BackColor = (!cb.Checked && _animState) ? Color.LightYellow : SystemColors.Control;
        }

        private void CheckSkillProgress(object sender, EventArgs e)
        {
            if (!_animTimer.Enabled) return;
            if (_pendingCheckBoxes.Count(cb => cb.Checked) >= _pendingSkillCount)
                StopSkillAnimation();
        }

        // ──────────────────────────────────────────────────────────────────────────

        public void SetClassLevel(Character character, int classId, int newMaxLevel, DDInformationContext db)
        {
            var currentLevels = character.Levels
                .Where(l => l.IdClass == classId)
                .ToList();

            int currentMax = currentLevels.Any() ? currentLevels.Max(l => l.Level1) : 0;

            if (newMaxLevel > currentMax)
            {
                var toAdd = db.Levels
                    .Where(l => l.IdClass == classId && l.Level1 > currentMax && l.Level1 <= newMaxLevel)
                    .ToList();

                foreach (var level in toAdd)
                    character.Levels.Add(level);
            }
            else if (newMaxLevel < currentMax)
            {
                var toRemove = currentLevels
                    .Where(l => l.Level1 > newMaxLevel)
                    .ToList();

                foreach (var level in toRemove)
                    character.Levels.Remove(level);
            }

            db.SaveChanges();
        }
    }
}
