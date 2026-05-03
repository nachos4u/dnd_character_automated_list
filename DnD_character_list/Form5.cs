using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form5 : Form
    {
        private TableLayoutPanel mainTable;
        private Button btnAddGroup;
        private Button btnOK;
        private Button btnCancel;
        private List<GroupOfClasses> groups = new List<GroupOfClasses>();
        public List<int> SelectedClassIds { get; private set; }
        public List<int> SelectedLevels { get; private set; }

        public Form5()
        {
            InitializeComponent();
            SelectedClassIds = new List<int>();
            SelectedLevels = new List<int>();
            SetupForm();
        }

        private void SetupForm()
        {
            this.Size = new Size(700, 550);

            Panel topPanel = new Panel();
            topPanel.Height = 50;
            topPanel.Dock = DockStyle.Top;

            btnAddGroup = new Button();
            btnAddGroup.Text = "➕ Добавить группу";
            btnAddGroup.Size = new Size(150, 35);
            btnAddGroup.Location = new Point(10, 10);
            btnAddGroup.Click += BtnAddGroup_Click;

            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Size = new Size(80, 30);
            btnOK.Location = new Point(600, 10);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(500, 10);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(80, 30);

            topPanel.Controls.AddRange(new Control[] { btnAddGroup, btnOK, btnCancel });

            mainTable = new TableLayoutPanel();
            mainTable.Dock = DockStyle.Fill;
            mainTable.AutoScroll = true;
            mainTable.ColumnCount = 1;
            mainTable.RowCount = 0;
            mainTable.Padding = new Padding(10);

            Panel scrollPanel = new Panel();
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.AutoScroll = true;
            scrollPanel.Controls.Add(mainTable);

            Controls.Add(scrollPanel);
            Controls.Add(topPanel);
        }

        private void BtnAddGroup_Click(object sender, EventArgs e)
        {
            GroupOfClasses group = new GroupOfClasses();

            group.OnDelete += (g) =>
            {
                mainTable.Controls.Remove(g.Panel);
                groups.Remove(g);
                g.Dispose();
                ReindexGroups();
            };

            groups.Add(group);
            mainTable.Controls.Add(group.Panel);
            mainTable.RowCount = groups.Count;
            mainTable.Height = groups.Count * 120;
        }

        private void ReindexGroups()
        {
            for (int i = 0; i < groups.Count; i++)
                groups[i].Index = i + 1;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (this.DialogResult == DialogResult.OK)
            {
                foreach (var group in groups)
                {
                    SelectedClassIds.Add(group.ClassId);
                    SelectedLevels.Add(group.Level);
                }
            }
        }
    }
}
