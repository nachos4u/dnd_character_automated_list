using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    /// <summary>
    /// Modal form for picking feats/traits. Uses virtual GDI+ rendering to support
    /// unlimited number of items without hitting the WinForms HWND limit.
    /// </summary>
    public class TraitPickerForm : Form
    {
        // ── Layout constants ─────────────────────────────────────────────────────
        private const int CardW  = 560;
        private const int CardH  = 110;
        private const int PadL   = 10;
        private const int PadT   = 10;
        private const int Gap    = 8;

        // ── Controls ─────────────────────────────────────────────────────────────
        private TextBox   _searchBox   = null!;
        private Panel     _canvas      = null!;
        private VScrollBar _vbar       = null!;
        private Button    _confirmBtn  = null!;
        private Button    _cancelBtn   = null!;
        private Label     _countLabel  = null!;

        // ── Data ─────────────────────────────────────────────────────────────────
        private List<Trait> _allTraits       = new();
        private List<Trait> _filteredTraits  = new();
        private HashSet<int> _selected       = new();
        private int          _scrollY        = 0;

        // ── Public result ─────────────────────────────────────────────────────────
        public List<int> SelectedTraitIds { get; private set; } = new();

        // ─────────────────────────────────────────────────────────────────────────
        public TraitPickerForm(IEnumerable<int> alreadySelected)
        {
            _selected = new HashSet<int>(alreadySelected);

            Text            = "Выбор черт";
            Size            = new Size(620, 760);
            MinimumSize     = new Size(620, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildUI();
            LoadTraits();
        }

        // ── UI construction ───────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Top filter panel ─────────────────────────────────────────────────
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 44 };

            var searchLbl = new Label
            {
                Text     = "Поиск:",
                Location = new Point(6, 13),
                AutoSize = true
            };

            _searchBox = new TextBox
            {
                Location = new Point(64, 10),
                Width    = 320,
                PlaceholderText = "название черты..."
            };
            _searchBox.TextChanged += (_, __) => ApplyFilters();

            filterPanel.Controls.AddRange(new Control[] { searchLbl, _searchBox });

            // ── Bottom panel with count + buttons ────────────────────────────────
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            _countLabel = new Label
            {
                AutoSize  = true,
                Location  = new Point(6, 14),
                ForeColor = Color.Gray
            };

            _confirmBtn = new Button
            {
                Text     = "Добавить",
                Anchor   = AnchorStyles.Right | AnchorStyles.Bottom,
                Size     = new Size(100, 28),
                Location = new Point(bottomPanel.Width - 220, 8)
            };
            _confirmBtn.Anchor  = AnchorStyles.Right | AnchorStyles.Bottom;
            _confirmBtn.Click  += ConfirmBtn_Click;

            _cancelBtn = new Button
            {
                Text     = "Отмена",
                Size     = new Size(90, 28),
                Location = new Point(bottomPanel.Width - 110, 8)
            };
            _cancelBtn.Anchor  = AnchorStyles.Right | AnchorStyles.Bottom;
            _cancelBtn.Click  += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            bottomPanel.Controls.AddRange(new Control[] { _countLabel, _confirmBtn, _cancelBtn });

            // ── Canvas + scrollbar ────────────────────────────────────────────────
            _vbar = new VScrollBar
            {
                Dock         = DockStyle.Right,
                SmallChange  = CardH + Gap,
                LargeChange  = 600
            };
            _vbar.Scroll += (_, e) => { _scrollY = _vbar.Value; _canvas.Invalidate(); };

            _canvas = new Panel
            {
                Dock            = DockStyle.Fill,
                BackColor       = Color.White,
                AllowDrop       = false
            };
            SetDoubleBuffered(_canvas);

            _canvas.Paint         += (_, e) => PaintCards(e.Graphics);
            _canvas.MouseDown     += OnCanvasMouseDown;
            _canvas.MouseWheel    += OnCanvasMouseWheel;
            _canvas.MouseEnter    += (_, __) => _canvas.Focus();
            _canvas.Resize        += (_, __) => { UpdateVBarRange(); _canvas.Invalidate(); };

            Controls.Add(_canvas);
            Controls.Add(_vbar);
            Controls.Add(bottomPanel);
            Controls.Add(filterPanel);
        }

        // ── Data loading ──────────────────────────────────────────────────────────
        private void LoadTraits()
        {
            try
            {
                using var db = new DDInformationContext();
                _allTraits = db.Traits.OrderBy(t => t.Name ?? t.CharTics).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки черт: {ex.Message}", "Ошибка");
                _allTraits = new List<Trait>();
            }

            ApplyFilters();
        }

        // ── Filtering ─────────────────────────────────────────────────────────────
        private void ApplyFilters()
        {
            var q = _searchBox.Text.Trim().ToLower();

            _filteredTraits = _allTraits.Where(t =>
            {
                var displayName = t.Name ?? t.CharTics ?? "";
                if (!string.IsNullOrEmpty(q) && !displayName.ToLower().Contains(q))
                    return false;
                return true;
            }).ToList();

            RebuildList();
        }

        private void RebuildList()
        {
            _scrollY = 0;
            UpdateVBarRange();
            _canvas.Invalidate();
            _countLabel.Text = $"Найдено: {_filteredTraits.Count}";
        }

        // ── VScrollBar ────────────────────────────────────────────────────────────
        private void UpdateVBarRange()
        {
            int total    = _filteredTraits.Count * (CardH + Gap) + PadT * 2;
            int visibleH = _canvas.Height;

            if (total <= visibleH)
            {
                _vbar.Maximum  = 0;
                _vbar.Value    = 0;
                _scrollY       = 0;
                return;
            }

            int maxScroll    = total - visibleH;
            _vbar.Minimum    = 0;
            _vbar.Maximum    = maxScroll + visibleH - 1;
            _vbar.LargeChange = visibleH;
            _vbar.SmallChange = CardH + Gap;
            _vbar.Value       = Math.Min(_scrollY, maxScroll);
            _scrollY          = _vbar.Value;
        }

        // ── Painting ──────────────────────────────────────────────────────────────
        private void PaintCards(Graphics g)
        {
            g.Clear(Color.White);

            if (_filteredTraits.Count == 0)
            {
                if (_allTraits.Count == 0)
                {
                    var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
                    var hintFont  = new Font("Segoe UI", 9f);
                    g.DrawString("Черты не загружены", titleFont, Brushes.DimGray, 30, 30);
                    g.DrawString("Перейдите в меню «База данных» и нажмите «Обновить черты»,", hintFont, Brushes.Gray, 30, 60);
                    g.DrawString("чтобы импортировать черты с ttg.club.", hintFont, Brushes.Gray, 30, 80);
                    titleFont.Dispose();
                    hintFont.Dispose();
                }
                else
                {
                    g.DrawString("Ничего не найдено", SystemFonts.DefaultFont, Brushes.Gray, 20, 20);
                }
                return;
            }

            int step    = CardH + Gap;
            int visible = _canvas.Height;
            int first   = Math.Max(0, (_scrollY - PadT) / step);
            int last    = Math.Min(_filteredTraits.Count - 1, first + visible / step + 2);

            var state = g.Save();
            for (int i = first; i <= last; i++)
            {
                int y = PadT + i * step - _scrollY;
                if (y + CardH < 0 || y > visible) continue;

                g.Restore(state);
                state = g.Save();
                g.SetClip(new Rectangle(PadL, y, CardW, CardH));
                g.TranslateTransform(PadL, y);
                DrawCard(g, _filteredTraits[i], CardW, CardH, _selected.Contains(_filteredTraits[i].IdTrait));
                g.TranslateTransform(-PadL, -y);
            }
            g.Restore(state);
        }

        private static void DrawCard(Graphics g, Trait trait, int W, int H, bool selected)
        {
            // Background
            var bgColor = selected ? Color.FromArgb(230, 245, 230) : Color.White;
            g.FillRectangle(new SolidBrush(bgColor), 0, 0, W, H);
            g.DrawRectangle(selected ? new Pen(Color.Green, 2) : Pens.LightGray, 0, 0, W - 1, H - 1);

            // Name
            string displayName = trait.Name ?? trait.CharTics ?? "(без названия)";
            var nameFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString(displayName, nameFont, Brushes.Black, new Rectangle(6, 4, W - 12, 22),
                new StringFormat { Trimming = StringTrimming.EllipsisCharacter });

            // Requirements
            if (!string.IsNullOrWhiteSpace(trait.Requirements))
            {
                var reqFont  = new Font("Segoe UI", 8f);
                var reqColor = Color.FromArgb(140, 100, 20);
                g.DrawString($"Требования: {trait.Requirements}", reqFont, new SolidBrush(reqColor),
                    new Rectangle(6, 22, W - 12, 16),
                    new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }

            // Source
            if (!string.IsNullOrWhiteSpace(trait.Source))
            {
                var srcFont = new Font("Segoe UI", 7.5f);
                var srcStr  = $"[{trait.Source}]";
                var srcSize = g.MeasureString(srcStr, srcFont);
                g.DrawString(srcStr, srcFont, Brushes.Gray, W - srcSize.Width - 6, 4);
            }

            // Description (clipped to remaining height)
            int descY = string.IsNullOrWhiteSpace(trait.Requirements) ? 24 : 40;
            var descFont = new Font("Segoe UI", 8.5f);
            g.DrawString(trait.Description ?? "", descFont, Brushes.DarkSlateGray,
                new RectangleF(6, descY, W - 12, H - descY - 6));

            nameFont.Dispose();
        }

        // ── Mouse events ──────────────────────────────────────────────────────────
        private void OnCanvasMouseDown(object? sender, MouseEventArgs me)
        {
            if (me.Button != MouseButtons.Left) return;

            int step = CardH + Gap;
            int idx  = (me.Y + _scrollY - PadT) / step;
            if (idx < 0 || idx >= _filteredTraits.Count) return;

            var trait = _filteredTraits[idx];
            if (_selected.Contains(trait.IdTrait))
                _selected.Remove(trait.IdTrait);
            else
                _selected.Add(trait.IdTrait);

            _canvas.Invalidate();
        }

        private void OnCanvasMouseWheel(object? sender, MouseEventArgs me)
        {
            int delta = -me.Delta / 120 * (CardH + Gap);
            int total = _filteredTraits.Count * (CardH + Gap) + PadT * 2;
            _scrollY  = Math.Max(0, Math.Min(_scrollY + delta, Math.Max(0, total - _canvas.Height)));

            if (_vbar.Maximum > 0)
                _vbar.Value = Math.Min(_scrollY, _vbar.Maximum - _vbar.LargeChange + 1);

            _canvas.Invalidate();
        }

        // ── Confirm ───────────────────────────────────────────────────────────────
        private void ConfirmBtn_Click(object? sender, EventArgs e)
        {
            SelectedTraitIds = _selected.ToList();
            DialogResult     = DialogResult.OK;
            Close();
        }

        // ── Helper ────────────────────────────────────────────────────────────────
        private static void SetDoubleBuffered(Control c)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)!
                .SetValue(c, true);
        }
    }
}
