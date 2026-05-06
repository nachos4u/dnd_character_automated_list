using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form2 : Form
    {
        private int IDPrewiosPage;

        public Form2(int value)
        {
            InitializeComponent();
            IDPrewiosPage = value;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(IDPrewiosPage);
            form4.Show();
            this.Hide();
        }

        private void label1_Click(object sender, EventArgs e) { }

        private void label2_Click(object sender, EventArgs e) { }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
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

        private async void SpesiesButton_Click(object sender, EventArgs e)
        {
            var importer = new SpeciesImporter();
            await importer.ImportRacesAsync();
        }

        private async void BackgroundButton_Click(object sender, EventArgs e)
        {
            var importer = new BackgroundImporter();
            await importer.ImportBackgroundAsync();
        }

        private async void ClassesButton_Click(object sender, EventArgs e)
        {
            var importer = new ClassesImporter();
            await importer.ImportClassAsync();
        }

        private async void SpellsButton_Click(object sender, EventArgs e)
        {
            var importer = new SpellsImporter();
            await importer.ImportSpellAsync();
        }
    }
}
