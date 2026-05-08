using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            // ВАЖНО: сначала добавляем label и textbox, потом pictureBox.SendToBack()
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
            newPb.SendToBack(); // вызываем ПОСЛЕ добавления всех трёх

            // Настраиваем диапазоны для вычисляемых NumericUpDown
            InitiativaUpDown.Minimum = -10;
            InitiativaUpDown.Maximum = 20;
            PassivPerceptionUpDown.Minimum = 1;
            PassivPerceptionUpDown.Maximum = 40;

            // Кнопки JSON рядом с (теперь скрытой) кнопкой Reload
            AddJsonButtons();

            // Скрываем кнопку Reload — сохранение теперь автоматическое
            ReloadButton.Visible = false;

            this.pictureBox2.MouseClick += pictureBox2_Click;
            _animTimer.Tick += AnimTimer_Tick;

            WireAutoSave();
            Load_character();
        }

        // ─── Добавление кнопок JSON ────────────────────────────────────────────────
        private void AddJsonButtons()
        {
            var exportJsonBtn = new Button
            {
                Text = "Экспорт JSON",
                Location = new Point(636, 135),
                Size = new Size(149, 29),
                UseVisualStyleBackColor = false
            };
            exportJsonBtn.Click += ExportJsonButton_Click;

            var importJsonBtn = new Button
            {
                Text = "Импорт JSON",
                Location = new Point(636, 100),
                Size = new Size(149, 29),
                UseVisualStyleBackColor = false
            };
            importJsonBtn.Click += ImportJsonButton_Click;

            var exportPdfBtn = new Button
            {
                Text = "Экспорт PDF",
                Location = new Point(636, 65),
                Size = new Size(149, 29),
                UseVisualStyleBackColor = false
            };
            exportPdfBtn.Click += ExportPdfButton_Click;

            this.Controls.Add(exportJsonBtn);
            this.Controls.Add(importJsonBtn);
            this.Controls.Add(exportPdfBtn);
        }

        // ─── JSON экспорт / импорт ─────────────────────────────────────────────────
        private void ExportJsonButton_Click(object sender, EventArgs e)
        {
            var serializer = new CharacterJsonSerializer();
            string json = serializer.ExportToJson(DataIDCharacter);
            if (json == null) { MessageBox.Show("Персонаж не найден", "Ошибка"); return; }

            using var dlg = new SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                FileName = $"character_{DataIDCharacter}.json",
                Title = "Экспорт персонажа"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, json, System.Text.Encoding.UTF8);
                MessageBox.Show("Персонаж экспортирован в JSON!", "Успех");
            }
        }

        private void ImportJsonButton_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                Title = "Импорт персонажа"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string json = File.ReadAllText(dlg.FileName, System.Text.Encoding.UTF8);
            var serializer = new CharacterJsonSerializer();
            if (serializer.ImportFromJson(json, out string err))
                MessageBox.Show(
                    "Персонаж импортирован!\nОткройте список персонажей, чтобы выбрать его.",
                    "Успех");
            else
                MessageBox.Show($"Ошибка импорта:\n{err}", "Ошибка");
        }

        // ─── Базовые события формы ─────────────────────────────────────────────────
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

        // ─── Обёртка для совместимости с Designer (кнопка скрыта) ────────────────
        private void ReloadButton_Click(object sender, EventArgs e) => SaveCharacterToDb();

        // ─── Кнопка уровня класса ──────────────────────────────────────────────────
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

                        // Fix 5: если классов нет — очищаем и выходим
                        if (!chosenClasesIds.Any())
                        {
                            ClassNameTextBox.Text = "";
                            FillClassInfo(character);
                            return;
                        }

                        // Fix 1: полное имя класса (с подклассом)
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
                                            var ch = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                                            var back = db.Backgrounds.FirstOrDefault(b => b.IdBackground == ch.IdBackground);
                                            var backPoss = back?.Possesion?.Split(";") ?? Array.Empty<string>();
                                            classPossOld = po[2].Split(", ").ToList();
                                            foreach (var cls in classPossOld)
                                            {
                                                if (!backPoss.Contains(cls))
                                                    classPoss.Add(cls);
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

                        foreach (var spas in classSpas.Split(", "))
                        {
                            switch (spas.Trim())
                            {
                                case "Сила":        StrengthCheckBox.Checked = true;     break;
                                case "Ловкость":    AgilityCheckBox.Checked = true;      break;
                                case "Телосложение": StaminaCheckBox.Checked = true;     break;
                                case "Интеллект":   IntelligenceCheckBox.Checked = true; break;
                                case "Мудрость":    WisdomCheckBox.Checked = true;       break;
                                case "Харизма":     CharismaCheckBox.Checked = true;     break;
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

                        FillClassInfo(character);
                        UpdateCalculatedValues();
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
