using System;
using System.Drawing;
using System.Windows.Forms;

namespace DnD_character_list
{
    /// <summary>
    /// Modeless progress window shown while importers scrape the site.
    /// Always update via <see cref="ReportProgress"/> — it is always called
    /// on the UI thread through <see cref="Progress{T}"/>.
    /// </summary>
    public class ImportProgressForm : Form
    {
        private readonly Label       _statusLabel;
        private readonly ProgressBar _progressBar;
        private readonly Label       _nameLabel;

        public ImportProgressForm()
        {
            Text            = "Импорт данных";
            Size            = new Size(500, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;

            _statusLabel = new Label
            {
                Text      = "Подготовка...",
                Location  = new Point(16, 16),
                Size      = new Size(460, 28),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(16, 52),
                Size     = new Size(460, 24),
                Minimum  = 0,
                Maximum  = 1,
                Value    = 0,
                Style    = ProgressBarStyle.Continuous
            };

            _nameLabel = new Label
            {
                Text         = "",
                Location     = new Point(16, 84),
                Size         = new Size(460, 20),
                Font         = new Font("Segoe UI", 8.5f),
                ForeColor    = Color.DimGray,
                TextAlign    = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };

            Controls.AddRange(new Control[] { _statusLabel, _progressBar, _nameLabel });
        }

        /// <summary>
        /// Update the displayed progress. Always called on the UI thread via Progress&lt;T&gt;.
        /// </summary>
        public void ReportProgress(int current, int total, string name)
        {
            if (IsDisposed) return;
            _statusLabel.Text    = $"Обработано: {current} / {total}";
            _progressBar.Maximum = Math.Max(1, total);
            _progressBar.Value   = Math.Min(current, Math.Max(1, total));
            _nameLabel.Text      = name;
        }
    }
}
