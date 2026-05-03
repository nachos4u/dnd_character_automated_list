namespace DnD_character_list
{
    internal class GroupOfCharecters : IDisposable
    {
        public Panel Panel { get; private set; }
        public Button Button { get; private set; }
        public Button DeleteButton { get; private set; }
        public int CharacterId { get; set; }

        private readonly Form1 _form1;

        public event Action<GroupOfCharecters> OnDelete;
        public event Action<GroupOfCharecters> OnOpen;

        public GroupOfCharecters(int characterId, string name, Form1 form1)
        {
            CharacterId = characterId;
            _form1 = form1;

            Panel = new Panel();
            Panel.Size = new Size(200, 80);
            Panel.Margin = new Padding(5);
            Panel.BorderStyle = BorderStyle.FixedSingle;

            Button = new Button();
            Button.Size = new Size(150, 60);
            Button.Location = new Point(5, 10);
            Button.Text = string.IsNullOrEmpty(name) ? "Безымянный" : name;
            Button.Click += Button_Click;

            DeleteButton = new Button();
            DeleteButton.Text = "🗑️";
            DeleteButton.Size = new Size(35, 60);
            DeleteButton.Location = new Point(158, 10);
            DeleteButton.BackColor = Color.LightCoral;
            DeleteButton.Click += DeleteButton_Click;

            Panel.Controls.AddRange(new Control[] { Button, DeleteButton });
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
