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
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new SpeciesImporter().ImportRacesAsync(progress);

            progressForm.Close();
        }

        private async void BackgroundButton_Click(object sender, EventArgs e)
        {
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new BackgroundImporter().ImportBackgroundAsync(progress);

            progressForm.Close();
        }

        private async void ClassesButton_Click(object sender, EventArgs e)
        {
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new ClassesImporter().ImportClassAsync(progress);

            progressForm.Close();
        }

        private async void SpellsButton_Click(object sender, EventArgs e)
        {
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new SpellsImporter().ImportSpellAsync(progress);

            progressForm.Close();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new SpellsImporter().ImportSpellAsync(progress);
            await new ClassesImporter().ImportClassAsync(progress);
            await new BackgroundImporter().ImportBackgroundAsync(progress);
            await new SpeciesImporter().ImportRacesAsync(progress);
            await new ItemsImporter().ImportItemsAsync(progress);
            await new FeatsImporter().ImportFeatsAsync(progress);

            progressForm.Close();
        }

        private async void InvetoryButton_Click(object sender, EventArgs e)
        {
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new ItemsImporter().ImportItemsAsync(progress);

            progressForm.Close();
        }

        private async void TraitsButton_Click(object sender, EventArgs e)
        {
            using var progressForm = new ImportProgressForm();
            progressForm.Show(this);

            var progress = new Progress<(int current, int total, string name)>(p =>
                progressForm.ReportProgress(p.current, p.total, p.name));

            await new FeatsImporter().ImportFeatsAsync(progress);

            progressForm.Close();
        }
    }
}
