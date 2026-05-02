using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static DnD_character_list.Form3;

namespace DnD_character_list
{
    internal class GroupOfCharecters : IDisposable
    {
        public Button Button { get; private set; }
        public Label Label { get; private set; }
        public Button DeleteButton { get; private set; }
        public Panel Panel { get; private set; }
        public GroupBox GroupBox { get; private set; }
        public int Index { get; set; }
        private bool _isClosed { get; set; } = false;
        public bool IsClosed
        {
            get => _isClosed;
            set
            {
                if (_isClosed)
                {
                    OnPropertyChanged();
                }
            }
        }

        public event Action<GroupOfCharecters> OnDelete;

        public GroupOfCharecters()
        {
            

            // Основная кнопка персонажа
            Button = new Button();
            Button.Size = new Size(180, 50);
            Button.Location = new Point(10, 12);
            Button.Text = $"Персонаж {Index}";
            Button.Click += Button_Click;

            // Панель для группы
            Panel = new Panel();
            Panel.Size = new Size(200, 70);
            Panel.Margin = new Padding(0, 0, 0, 10);

            GroupBox = new GroupBox();
            GroupBox.Size = new Size(200, 70);
            GroupBox.Location = new Point(0, 0);

            // Label с информацией
            Label = new Label();
            Label.Location = new Point(190, 10);
            Label.Size = new Size(200, 25);
            Label.Text = "Информация о персонаже";
            Label.TextAlign = ContentAlignment.MiddleLeft;

            // Кнопка удаления
            DeleteButton = new Button();
            DeleteButton.Text = "🗑️";
            DeleteButton.Size = new Size(10, 10);
            DeleteButton.Location = new Point(190, 10);
            DeleteButton.Click += DeleteButton_Click;
            DeleteButton.BackColor = Color.LightCoral;

            GroupBox.Controls.AddRange(new Control[] { Button, Label, DeleteButton });
            Panel.Controls.Add(GroupBox);

        }

        private void Button_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(Index);
            form4.Show();
            IsClosed = true;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            OnDelete?.Invoke(this);
        }

        public void Dispose()
        {
            Button?.Dispose();
            Label?.Dispose();
            DeleteButton?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
