using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DnD_character_list.Form5;

namespace DnD_character_list
{
    internal class GroupOfClasses : IDisposable
    {
        public Panel Panel { get; private set; }
        public GroupBox GroupBox { get; private set; }
        public TextBox TextBox { get; private set; }
        public NumericUpDown NumericUpDown { get; private set; }
        public ComboBox ComboBox { get; private set; }
        public int ClassId { get; set; }
        public int Level { get; set; }
        public int Index { get; set; }

        public event Action<GroupOfClasses> OnDelete;

        public GroupOfClasses()
        {
            using (var db = new DDInformationContext())
            {
                var class_list = db.Classes.ToList();
                Panel = new Panel();
                Panel.Size = new Size(650, 300);
                Panel.Margin = new Padding(0, 0, 0, 10);

                GroupBox = new GroupBox();
                GroupBox.Size = new Size(630, 290);
                GroupBox.Location = new Point(0, 0);

                ComboBox = new ComboBox();
                ComboBox.Location = new Point(250, 30);
                ComboBox.Size = new Size(220, 23);
                ComboBox.DataSource = class_list;
                ComboBox.DisplayMember = "Name";
                ComboBox.ValueMember = "IdClass";
                ComboBox.SelectedItem = db.Classes.FirstOrDefault(ci => ci.IdClass == 1);
                ComboBox.SelectedValueChanged += ComboBox_SelectedValueChanged;

                var selectedClass = ComboBox.SelectedItem as Class;
                var selectedClassId = selectedClass?.IdClass ?? 1;
                ClassId = selectedClass?.IdClass ?? 1;

                var classInfo = db.Classes.FirstOrDefault(ci => ci.IdClass == selectedClassId);

                // Текстовое поле
                Label lblText = new Label();
                lblText.Text = "Описание класса:";
                lblText.Location = new Point(15, 90);
                lblText.Size = new Size(90, 60);

                TextBox = new TextBox();
                TextBox.Multiline = true;
                TextBox.Location = new Point(120, 90);
                TextBox.Size = new Size(360, 180);
                TextBox.Text = classInfo.Description ?? "";

                // NumericUpDown
                Label lblNum = new Label();
                lblNum.Text = "Кол-во уровня:";
                lblNum.Location = new Point(15, 30);
                lblNum.Size = new Size(90, 60);

                NumericUpDown = new NumericUpDown();
                NumericUpDown.Location = new Point(120, 30);
                NumericUpDown.Size = new Size(120, 23);
                NumericUpDown.Minimum = 1;
                NumericUpDown.Maximum = 20;
                NumericUpDown.Value = 1;
                Level = (int)NumericUpDown.Value;
                NumericUpDown.ValueChanged += NumericUpDown_ValueChanged;


                // Кнопка удаления
                Button btnDelete = new Button();
                btnDelete.Text = "Удалить группу";
                btnDelete.Location = new Point(500, 45);
                btnDelete.Size = new Size(110, 30);
                btnDelete.BackColor = Color.Orange;
                btnDelete.Click += (s, e) => OnDelete?.Invoke(this);

                GroupBox.Controls.AddRange(new Control[]
                {
                lblText, TextBox, lblNum, NumericUpDown, btnDelete, ComboBox
                });

                Panel.Controls.Add(GroupBox);
            }


        }

        public void SetValues(int classId, int level)
        {
            var dataSource = ComboBox.DataSource as List<Class>;
            if (dataSource != null)
            {
                int idx = dataSource.FindIndex(c => c.IdClass == classId);
                if (idx >= 0)
                {
                    ComboBox.SelectedIndex = idx;
                    ClassId = classId;
                    TextBox.Text = dataSource[idx].Description ?? "";
                }
            }
            NumericUpDown.Value = Math.Max(NumericUpDown.Minimum, Math.Min(NumericUpDown.Maximum, level));
            Level = (int)NumericUpDown.Value;
        }

        private void NumericUpDown_ValueChanged(object? sender, EventArgs e)
        {
            Level = (int)NumericUpDown.Value;
        }

        private void ComboBox_SelectedValueChanged(object? sender, EventArgs e)
        {
            var selectedClass = ComboBox.SelectedItem as Class;
            ClassId = selectedClass?.IdClass ?? 1;
        }

        public void Dispose()
        {
            Panel?.Dispose();
            GroupBox?.Dispose();
        }
    }
}
