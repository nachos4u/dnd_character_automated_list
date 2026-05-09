using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    /// <summary>
    /// Modal form for picking items with quantities.
    /// Virtual GDI+ rendering. Rarity filter is a visible CheckedListBox on the right.
    /// </summary>
    public class ItemPickerForm : Form
    {
        // ── Layout constants ─────────────────────────────────────────────────────
        private const int CardH = 100;
        private const int PadL  = 8;
        private const int PadT  = 8;
        private const int Gap   = 6;

        private static readonly string[] KnownRarities =
        {
            "не магические", "обычный", "необычный", "редкое",
            "очень редкий",  "легендарное", "артефакт",
            "редкость не определена", "редкость варьируется"
        };

        // ── Controls ─────────────────────────────────────────────────────────────
        private TextBox        _searchBox   = null!;
        private CheckedListBox _rarityList  = null!;
        private Panel          _canvas      = null!;
        private VScrollBar     _vbar        = null!;
        private Button         _confirmBtn  = null!;
        private Button         _cancelBtn   = null!;
        private Label          _countLabel  = null!;

        // ── Data ─────────────────────────────────────────────────────────────────
        private List<Item>           _allItems     = new();
        private List<Item>           _filteredItems = new();
        private Dictionary<int, int> _selectedQty  = new();
        private int                  _scrollY       = 0;

        // ── Public result ─────────────────────────────────────────────────────────
        public Dictionary<int, int> SelectedItemQuantities { get; private set; } = new();

        // ─────────────────────────────────────────────────────────────────────────
        public ItemPickerForm(IEnumerable<(int id, int qty)> alreadySelected)
        {
            foreach (var (id, qty) in alreadySelected)
                if (qty > 0) _selectedQty[id] = qty;

            Text            = "Выбор предметов";
            Size            = new Size(860, 760);
            MinimumSize     = new Size(700, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildUI();
            LoadItems();
        }

        // ── UI construction ───────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Top search bar ────────────────────────────────────────────────────
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 38 };

            var searchLbl = new Label { Text = "Поиск:", Location = new Point(6, 11), AutoSize = true };
            _searchBox = new TextBox { Location = new Point(52, 8), Width = 340, PlaceholderText = "название предмета..." };
            _searchBox.TextChanged += (_, __) => ApplyFilters();
            topPanel.Controls.AddRange(new Control[] { searchLbl, _searchBox });

            // ── Bottom panel ──────────────────────────────────────────────────────
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            _countLabel = new Label { AutoSize = true, Location = new Point(6, 13), ForeColor = Color.Gray };

            _confirmBtn = new Button { Text = "Добавить", Size = new Size(100, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            _cancelBtn  = new Button { Text = "Отмена",   Size = new Size(90,  28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };

            _confirmBtn.Click += ConfirmBtn_Click;
            _cancelBtn.Click  += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            bottomPanel.Controls.AddRange(new Control[] { _countLabel, _confirmBtn, _cancelBtn });
            bottomPanel.Resize += (_, __) =>
            {
                _confirmBtn.Location = new Point(bottomPanel.Width - 208, 6);
                _cancelBtn.Location  = new Point(bottomPanel.Width - 100, 6);
            };

            // ── Right panel: rarity filter ────────────────────────────────────────
            var rightPanel = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 170,
                BackColor = Color.FromArgb(248, 248, 252),
                Padding   = new Padding(4)
            };

            var rarityHeader = new Label
            {
                Text      = "Редкость:",
                Location  = new Point(4, 4),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            _rarityList = new CheckedListBox
            {
                Location     = new Point(4, 24),
                Size         = new Size(160, 400),
                CheckOnClick = true,
                BorderStyle  = BorderStyle.None,
                Font         = new Font("Segoe UI", 8f),
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
            };
            foreach (var r in KnownRarities)
                _rarityList.Items.Add(r, false);
            _rarityList.ItemCheck += (_, __) =>
            {
                // ItemCheck fires BEFORE the state changes, so defer by one tick
                BeginInvoke((Action)ApplyFilters);
            };

            rightPanel.Controls.AddRange(new Control[] { rarityHeader, _rarityList });

            // ── Scrollbar ─────────────────────────────────────────────────────────
            _vbar = new VScrollBar
            {
                Dock        = DockStyle.Right,
                SmallChange = CardH + Gap,
                LargeChange = 600
            };
            _vbar.Scroll += (_, e) => { _scrollY = _vbar.Value; _canvas.Invalidate(); };

            // ── Canvas ────────────────────────────────────────────────────────────
            _canvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            SetDoubleBuffered(_canvas);
            _canvas.Paint      += (_, e) => PaintCards(e.Graphics);
            _canvas.MouseDown  += OnCanvasMouseDown;
            _canvas.MouseWheel += OnCanvasMouseWheel;
            _canvas.MouseEnter += (_, __) => _canvas.Focus();
            _canvas.Resize     += (_, __) => { UpdateVBarRange(); _canvas.Invalidate(); };

            // Order matters for DockStyle: Fill goes first (lowest z), then others
            Controls.Add(_canvas);
            Controls.Add(_vbar);
            Controls.Add(rightPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        // ── Data loading ──────────────────────────────────────────────────────────
        private void LoadItems()
        {
            try
            {
                using var db = new DDInformationContext();
                _allItems = db.Items.OrderBy(i => i.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки предметов:\n{ex.Message}", "Ошибка");
                _allItems = new List<Item>();
            }
            ApplyFilters();
        }

        // ── Filtering ─────────────────────────────────────────────────────────────
        private void ApplyFilters()
        {
            if (_canvas == null) return;

            var q        = _searchBox?.Text.Trim().ToLower() ?? "";
            var rarities = _rarityList?.CheckedItems.Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase)
                           ?? new HashSet<string>();

            _filteredItems = _allItems.Where(item =>
            {
                if (!string.IsNullOrEmpty(q) && !(item.Name ?? "").ToLower().Contains(q))
                    return false;
                if (rarities.Count > 0)
                {
                    var r = item.Rarity ?? (item.IsMagic ? "редкость не определена" : "не магические");
                    if (!rarities.Contains(r))
                        return false;
                }
                return true;
            }).ToList();

            _scrollY = 0;
            UpdateVBarRange();
            _canvas.Invalidate();
            if (_countLabel != null)
                _countLabel.Text = $"Найдено: {_filteredItems.Count}";
        }

        // ── VScrollBar ────────────────────────────────────────────────────────────
        private void UpdateVBarRange()
        {
            if (_canvas == null || _vbar == null) return;

            int total    = _filteredItems.Count * (CardH + Gap) + PadT * 2;
            int visibleH = _canvas.Height;

            if (total <= visibleH || visibleH <= 0)
            {
                _vbar.Maximum = 0; _vbar.Value = 0; _scrollY = 0;
                return;
            }

            int maxScroll     = total - visibleH;
            _vbar.Minimum     = 0;
            _vbar.Maximum     = maxScroll + visibleH - 1;
            _vbar.LargeChange = Math.Max(1, visibleH);
            _vbar.SmallChange = CardH + Gap;
            _vbar.Value       = Math.Min(_scrollY, Math.Max(0, maxScroll));
            _scrollY          = _vbar.Value;
        }

        // ── Painting ──────────────────────────────────────────────────────────────
        private void PaintCards(Graphics g)
        {
            g.Clear(Color.White);

            if (_filteredItems.Count == 0)
            {
                if (_allItems.Count == 0)
                {
                    using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
                    using var hintFont  = new Font("Segoe UI", 9f);
                    g.DrawString("Предметы не загружены", titleFont, Brushes.DimGray, 24, 24);
                    g.DrawString("Откройте «База данных» и нажмите «Обновить сокровища»,", hintFont, Brushes.Gray, 24, 56);
                    g.DrawString("чтобы импортировать предметы с ttg.club.", hintFont, Brushes.Gray, 24, 76);
                }
                else
                {
                    g.DrawString("Ничего не найдено", SystemFonts.DefaultFont, Brushes.Gray, 24, 24);
                }
                return;
            }

            int cardW   = Math.Max(200, _canvas.Width - PadL * 2 - 4);
            int step    = CardH + Gap;
            int visible = _canvas.Height;
            int first   = Math.Max(0, (_scrollY - PadT) / step);
            int last    = Math.Min(_filteredItems.Count - 1, first + visible / step + 2);

            for (int i = first; i <= last; i++)
            {
                int y = PadT + i * step - _scrollY;
                if (y + CardH < 0 || y > visible) continue;

                var item = _filteredItems[i];
                int qty  = _selectedQty.TryGetValue(item.IdItem, out var q) ? q : 0;
                try
                {
                    DrawCard(g, item, PadL, y, cardW, CardH, qty);
                }
                catch { /* skip card on GDI+ error */ }
            }
        }

        // offsetX/offsetY: top-left corner of the card in canvas coordinates
        private static void DrawCard(Graphics g, Item item, int offsetX, int offsetY, int W, int H, int qty)
        {
            bool selected = qty > 0;

            // Background
            using var bgBrush = new SolidBrush(selected ? Color.FromArgb(232, 242, 255) : Color.White);
            g.FillRectangle(bgBrush, offsetX, offsetY, W, H);
            using var borderPen = selected ? new Pen(Color.SteelBlue, 2) : new Pen(Color.LightGray);
            g.DrawRectangle(borderPen, offsetX, offsetY, W - 1, H - 1);

            // Name
            using var nameFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            int nameW = Math.Max(1, W - (selected ? 130 : 90));
            using var ellipsisFmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(item.Name ?? "(без названия)", nameFont, Brushes.Black,
                new Rectangle(offsetX + 6, offsetY + 4, nameW, 22), ellipsisFmt);

            // Rarity badge (top right)
            var rarity = item.Rarity ?? (item.IsMagic ? "редкость не определена" : "не магические");
            using var rarFont  = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            using var rarBrush = new SolidBrush(GetRarityColor(rarity));
            g.DrawString(rarity, rarFont, rarBrush,
                offsetX + W - (selected ? 124 : 84), offsetY + 6);

            // Meta: type | price | weight
            var meta = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.ItemType)) meta.Add(item.ItemType);
            if (!string.IsNullOrWhiteSpace(item.Price))    meta.Add($"Цена: {item.Price}");
            if (item.Weight.HasValue)                       meta.Add($"Вес: {item.Weight:0.##} фн.");
            if (meta.Count > 0)
            {
                using var metaFont = new Font("Segoe UI", 7.5f);
                g.DrawString(string.Join("  |  ", meta), metaFont, Brushes.DimGray,
                    new Rectangle(offsetX + 6, offsetY + 24, Math.Max(1, W - 12), 16),
                    ellipsisFmt);
            }

            // Separator
            g.DrawLine(Pens.LightGray, offsetX + 6, offsetY + 42, offsetX + W - 6, offsetY + 42);

            // Description — guard width/height to be at least 1
            using var descFont = new Font("Segoe UI", 8f);
            int descW = Math.Max(1, W - (selected ? 80 : 12));
            int descH = Math.Max(1, H - 52);
            g.DrawString(item.Description ?? "", descFont, Brushes.DarkSlateGray,
                new RectangleF(offsetX + 6, offsetY + 46, descW, descH));

            // Qty controls
            if (selected)
            {
                int bx = offsetX + W - 72;
                int by = offsetY + H / 2 - 14;

                using var minusBg = new SolidBrush(Color.FromArgb(255, 210, 210));
                using var plusBg  = new SolidBrush(Color.FromArgb(200, 240, 200));
                using var btnFont = new Font("Segoe UI", 12f, FontStyle.Bold);
                using var qtyFont = new Font("Segoe UI", 10f, FontStyle.Bold);
                using var centerFmt = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.FillRectangle(minusBg, bx, by, 24, 28);
                g.DrawRectangle(Pens.LightCoral, bx, by, 24, 28);
                g.DrawString("−", btnFont, Brushes.DarkRed, bx + 3, by + 2);

                g.DrawString(qty.ToString(), qtyFont, Brushes.Black,
                    new Rectangle(bx + 26, by, 18, 28), centerFmt);

                g.FillRectangle(plusBg, bx + 46, by, 24, 28);
                g.DrawRectangle(Pens.SeaGreen, bx + 46, by, 24, 28);
                g.DrawString("+", btnFont, Brushes.DarkGreen, bx + 49, by + 2);
            }
        }

        private static Color GetRarityColor(string r) => r?.ToLower() switch
        {
            "обычный"              => Color.Gray,
            "необычный"            => Color.ForestGreen,
            "редкое"               => Color.RoyalBlue,
            "очень редкий"         => Color.Purple,
            "легендарное"          => Color.OrangeRed,
            "артефакт"             => Color.Crimson,
            "редкость варьируется" => Color.Teal,
            _                      => Color.DimGray
        };

        // ── Mouse events ──────────────────────────────────────────────────────────
        private void OnCanvasMouseDown(object? sender, MouseEventArgs me)
        {
            if (me.Button != MouseButtons.Left) return;
            int step = CardH + Gap;
            int idx  = (me.Y + _scrollY - PadT) / step;
            if (idx < 0 || idx >= _filteredItems.Count) return;

            var item   = _filteredItems[idx];
            int cardY  = PadT + idx * step - _scrollY;
            int relX   = me.X - PadL;
            int relY   = me.Y - cardY;
            int id     = item.IdItem;
            int cardW  = Math.Max(200, _canvas.Width - PadL * 2 - 4);

            bool sel = _selectedQty.TryGetValue(id, out var qty) && qty > 0;
            if (sel)
            {
                int bx = cardW - 72;
                int by = CardH / 2 - 14;

                if (relX >= bx && relX <= bx + 24 && relY >= by && relY <= by + 28)
                {
                    _selectedQty[id] = Math.Max(0, qty - 1);
                    if (_selectedQty[id] == 0) _selectedQty.Remove(id);
                    _canvas.Invalidate(); return;
                }
                if (relX >= bx + 46 && relX <= bx + 70 && relY >= by && relY <= by + 28)
                {
                    _selectedQty[id] = qty + 1;
                    _canvas.Invalidate(); return;
                }
            }

            if (sel) _selectedQty.Remove(id);
            else     _selectedQty[id] = 1;
            _canvas.Invalidate();
        }

        private void OnCanvasMouseWheel(object? sender, MouseEventArgs me)
        {
            int delta = -me.Delta / 120 * (CardH + Gap);
            int total = _filteredItems.Count * (CardH + Gap) + PadT * 2;
            _scrollY  = Math.Max(0, Math.Min(_scrollY + delta, Math.Max(0, total - _canvas.Height)));
            if (_vbar.Maximum > 0)
                _vbar.Value = Math.Min(_scrollY, Math.Max(0, _vbar.Maximum - _vbar.LargeChange + 1));
            _canvas.Invalidate();
        }

        // ── Confirm ───────────────────────────────────────────────────────────────
        private void ConfirmBtn_Click(object? sender, EventArgs e)
        {
            SelectedItemQuantities = new Dictionary<int, int>(_selectedQty.Where(kv => kv.Value > 0));
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Helper ────────────────────────────────────────────────────────────────
        private static void SetDoubleBuffered(Control c)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(c, true);
        }
    }
}
