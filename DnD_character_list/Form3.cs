using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit.Internal;

namespace DnD_character_list
{
    public partial class Form3 : Form
    {
        private TableLayoutPanel mainTable;
        private Label label;
        private List<GroupOfCharecters> groups = new List<GroupOfCharecters>();
        private Button btnAddGroup;
        private BindingList<GroupOfCharecters> items;
        private PictureBox pictureBox1;
        public Form3(Form1 form1)
        {
            InitializeComponent();
            items = new BindingList<GroupOfCharecters>();

            // Подписка на изменения свойств каждого элемента
            items.ForEach(item => item.PropertyChanged += Item_PropertyChanged);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
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
                    "Подтверждение выход",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }

        }

        private void BtnAddGroup_Click(object sender, EventArgs e)
        {
            int groupIndex = groups.Count + 1;
            GroupOfCharecters group = new GroupOfCharecters();

            // Устанавливаем индекс и текст кнопки
            group.Index = groupIndex;

            // Привязываем событие удаления
            group.OnDelete += (g) =>
            {
                mainTable.Controls.Remove(g.Button);
                groups.Remove(g);
                g.Dispose();
                ReindexGroups();
            };

            groups.Add(group);
            mainTable.Controls.Add(group.Button);
            mainTable.RowCount = groups.Count;
            mainTable.Height = groups.Count * 90;

            // Автопрокрутка вниз
            mainTable.ScrollControlIntoView(group.Button);
        }

        private void ReindexGroups()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].Index = i + 1;
            }
        }


        private void Form3_Load(object sender, EventArgs e)
        {
            this.Size = new Size(700, 550);

            // Верхняя панель
            Panel topPanel = new Panel();
            topPanel.Height = 140;
            topPanel.Dock = DockStyle.Top;

            // PictureBox
            pictureBox1 = new PictureBox();
            pictureBox1.BackColor = Color.White;
            pictureBox1.BackgroundImageLayout = ImageLayout.Center;
            pictureBox1.Image = Properties.Resources.Untitled;
            pictureBox1.Location = new Point(10, 10);
            pictureBox1.Size = new Size(81, 81);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Click += pictureBox1_Click;

            // Label
            label = new Label();
            label.AutoSize = true;
            label.Location = new Point(10, 105);
            label.Text = "Ваши персонажи";

            // Кнопка добавления
            btnAddGroup = new Button();
            btnAddGroup.Text = "➕ Добавить группу";
            btnAddGroup.Size = new Size(150, 35);
            btnAddGroup.Location = new Point(100, 10);
            btnAddGroup.Click += BtnAddGroup_Click;

            topPanel.Controls.AddRange(new Control[] { btnAddGroup, pictureBox1, label });

            // FlowLayoutPanel для групп (вместо TableLayoutPanel - проще)
            FlowLayoutPanel flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.WrapContents = false;
            flowPanel.AutoScroll = true;
            flowPanel.Padding = new Padding(10);

            // Сохраняем ссылку на flowPanel вместо mainTable
            mainTable = new TableLayoutPanel(); // Если хотите использовать TableLayoutPanel
            mainTable.Dock = DockStyle.Fill;
            mainTable.AutoScroll = true;
            mainTable.AutoSize = true;
            mainTable.ColumnCount = 1;
            mainTable.RowCount = 0;
            mainTable.Padding = new Padding(10);

            // Панель с прокруткой
            Panel scrollPanel = new Panel();
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.AutoScroll = true;
            scrollPanel.Controls.Add(mainTable);

            // Добавляем на форму
            this.Controls.Add(scrollPanel);
            this.Controls.Add(topPanel);
        }
        
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Hide();
        }
    }
}
