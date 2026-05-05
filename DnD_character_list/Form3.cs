using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;

namespace DnD_character_list
{
    public partial class Form3 : Form
    {
        private FlowLayoutPanel flowPanel;
        private Label label;
        private List<GroupOfCharecters> groups = new List<GroupOfCharecters>();
        private Button btnAddGroup;
        private PictureBox pictureBox1;
        private Form1 _form1;

        public Form3(Form1 form1)
        {
            _form1 = form1;
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (_form1 != null)
                _form1.Show();
            else
                new Form1().Show();
            this.Hide();
        }

        private void Form3_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
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

        private void BtnAddGroup_Click(object sender, EventArgs e)
        {
            try
            {
                using (var db = new DDInformationContext())
                {
                    var character = new Character
                    {
                        IdSpecies = 1,
                        IdBackground = 1,
                        Name = "Новый персонаж"
                    };
                    db.Characters.Add(character);
                    db.SaveChanges();
                    AddCharacterCard(character.IdCharacter, character.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании персонажа: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddCharacterCard(int characterId, string name)
        {
            var group = new GroupOfCharecters(characterId, name, _form1);

            group.OnDelete += (g) =>
            {
                DialogResult confirm = MessageBox.Show(
                    $"Удалить персонажа \"{g.Button.Text}\"?",
                    "Подтверждение удаления",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Cancel) return;

                try
                {
                    using (var db = new DDInformationContext())
                    {
                        var character = db.Characters
                            .Include(c => c.ItemInventories)
                            .Include(c => c.IdSpells)
                            .Include(c => c.IdTraits)
                            .Include(c => c.Levels)
                            .FirstOrDefault(c => c.IdCharacter == g.CharacterId);

                        if (character != null)
                        {
                            db.ItemInventories.RemoveRange(character.ItemInventories);
                            character.IdSpells.Clear();
                            character.IdTraits.Clear();
                            character.Levels.Clear();
                            db.SaveChanges();
                            db.Characters.Remove(character);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                flowPanel.Controls.Remove(g.Panel);
                groups.Remove(g);
                g.Dispose();
            };

            group.OnOpen += (g) => this.Hide();

            groups.Add(group);
            flowPanel.Controls.Add(group.Panel);
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            this.Size = new Size(820, 550);

            Panel topPanel = new Panel();
            topPanel.Height = 120;
            topPanel.Dock = DockStyle.Top;

            pictureBox1 = new PictureBox();
            pictureBox1.BackColor = Color.White;
            pictureBox1.BackgroundImageLayout = ImageLayout.Center;
            pictureBox1.Image = Properties.Resources.Untitled;
            pictureBox1.Location = new Point(10, 10);
            pictureBox1.Size = new Size(81, 81);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Click += pictureBox1_Click;

            label = new Label();
            label.AutoSize = true;
            label.Location = new Point(10, 100);
            label.Font = new Font("Times New Roman", 9f, FontStyle.Regular);
            label.Text = "Ваши персонажи";

            btnAddGroup = new Button();
            btnAddGroup.Font = new Font("Times New Roman", 9f, FontStyle.Regular);
            btnAddGroup.Text = "➕ Добавить персонажа";
            btnAddGroup.Size = new Size(250, 35);
            btnAddGroup.Location = new Point(100, 10);
            btnAddGroup.Click += BtnAddGroup_Click;

            topPanel.Controls.AddRange(new Control[] { btnAddGroup, pictureBox1, label });

            flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.LeftToRight;
            flowPanel.WrapContents = true;
            flowPanel.AutoScroll = true;
            flowPanel.Padding = new Padding(10);

            this.Controls.Add(flowPanel);
            this.Controls.Add(topPanel);

            LoadCharactersFromDb();
        }

        private void LoadCharactersFromDb()
        {
            try
            {
                using (var db = new DDInformationContext())
                {
                    var characters = db.Characters.OrderBy(c => c.IdCharacter).ToList();
                    foreach (var character in characters)
                        AddCharacterCard(character.IdCharacter, character.Name ?? "Безымянный");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки персонажей: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
