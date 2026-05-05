using Microsoft.EntityFrameworkCore;

namespace DnD_character_list
{
    internal class GroupOfCharecters : IDisposable
    {
        public Panel Panel { get; private set; }
        public Button Button { get; private set; }
        public Button DeleteButton { get; private set; }
        public Label Label { get; private set; }
        public int CharacterId { get; set; }

        private readonly Form1 _form1;

        public event Action<GroupOfCharecters> OnDelete;
        public event Action<GroupOfCharecters> OnOpen;

        public GroupOfCharecters(int characterId, string name, Form1 form1)
        {
            using (var db = new DDInformationContext())
            {
                var character = db.Characters
                    .Include(c => c.Levels)
                    .ThenInclude(l => l.IdClassNavigation)
                    .FirstOrDefault(c => c.IdCharacter == characterId);

                CharacterId = characterId;
                _form1 = form1;
                string specieName = db.Species.FirstOrDefault(s => s.IdSpecies == character.IdSpecies)?.Name ?? "Неизвестно";
                var classLevels = character.Levels
                                    .GroupBy(l => l.IdClassNavigation)
                                    .Select(g => new { Class = g.Key, MaxLevel = g.Max(l => l.Level1) })
                                    .ToList();
                List<string> classInfo = new List<string>();
                foreach (var cl in classLevels)
                {
                    string className = db.Classes.FirstOrDefault(c => c.IdClass == cl.Class.IdClass)?.Name ?? "Неизвестно";
                    classInfo.Add($"{className} {(int?)cl.MaxLevel ?? 0}");
                }
                Panel = new Panel();
                Panel.Size = new Size(250, 170);
                Panel.Margin = new Padding(5);
                Panel.BorderStyle = BorderStyle.FixedSingle;

                Label = new Label();
                Label.Size = new Size(200, 85);
                Label.Location = new Point(5, 10);
                Label.Font = new Font("Times New Roman", 9f, FontStyle.Regular);
                Label.Text = string.Join(", ", classInfo) + 
                    (classInfo.LongCount() == 0 ? "Неизвестно - " : " - ") + 
                    (specieName.ToString() == "-" ? " " : specieName.ToString());

                Button = new Button();
                Button.Size = new Size(238, 85);
                Button.Location = new Point(5, 75);
                Button.Font = new Font("Times New Roman", 9f, FontStyle.Regular);
                Button.Text = string.IsNullOrEmpty(name) ? "Безымянный" : name;
                Button.Click += Button_Click;

                DeleteButton = new Button();
                DeleteButton.Text = "🗑️";
                DeleteButton.Size = new Size(35, 35);
                DeleteButton.Location = new Point(208, 10);
                DeleteButton.BackColor = Color.LightCoral;
                DeleteButton.Click += DeleteButton_Click;

                Panel.Controls.AddRange(new Control[] { Button, DeleteButton, Label });
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(CharacterId, _form1);
            form4.Show();
            OnOpen?.Invoke(this);
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            OnDelete?.Invoke(this);
        }

        public void Dispose()
        {
            Button?.Dispose();
            DeleteButton?.Dispose();
            Panel?.Dispose();
        }
    }
}
