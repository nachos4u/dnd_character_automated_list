using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    /// <summary>
    /// Модальное окно выбора заклинаний.
    /// Список — виртуальный рендеринг (один Panel + VScrollBar), без дочерних контролов на карточках.
    /// Фильтры Класс / Школа / Уровень — мультивыбор через всплывающий CheckedListBox.
    /// </summary>
    public class SpellPickerForm : Form
    {
        // ─── Результат ────────────────────────────────────────────────────────────
        public List<int> SelectedSpellIds { get; private set; } = new();

        // ─── Поля ─────────────────────────────────────────────────────────────────
        private List<Spell>           _allSpells      = new();
        private List<Spell>           _filteredSpells = new();
        private readonly HashSet<int> _selected       = new();

        private TextBox         _searchBox    = null!;
        private MultiDropButton _classFilter  = null!;
        private MultiDropButton _schoolFilter = null!;
        private MultiDropButton _levelFilter  = null!;
        private ComboBox        _sourceFilter = null!, _rangeFilter = null!;
        private Panel           _canvas       = null!;
        private VScrollBar      _vbar         = null!;
        private Label           _countLabel   = null!;
        private int             _scrollY;

        private const int CardH   = 155;
        private const int CardGap =  10;
        private const int PadL    =  10;

        // ─── Конструктор ──────────────────────────────────────────────────────────
        public SpellPickerForm(List<int> alreadySelectedIds)
        {
            foreach (var id in alreadySelectedIds) _selected.Add(id);

            using (var db = new DDInformationContext())
                _allSpells = db.Spells
                    .OrderBy(s => s.CellLevel).ThenBy(s => s.Name)
                    .ToList();

            BuildUI();
            PopulateFilters();
            ApplyFilters();
        }

        // ─── Построение UI ────────────────────────────────────────────────────────
        private void BuildUI()
        {
            Text          = "Выбор заклинаний";
            Size          = new Size(1280, 860);
            MinimumSize   = new Size(920, 520);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox   = false;
            BackColor     = Color.WhiteSmoke;

            // ── Панель фильтров (сверху) ──────────────────────────────────────────
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top, Height = 62, BackColor = Color.White
            };

            const int fy = 20;     // вертикальная позиция элементов
            int fx = 12;           // горизонтальный курсор

            // Поиск
            filterPanel.Controls.Add(MkLbl("Поиск:", fx, fy)); fx += 56;
            _searchBox = new TextBox
            {
                Location = new Point(fx, fy - 2), Size = new Size(178, 24),
                PlaceholderText = "Название заклинания…"
            };
            _searchBox.TextChanged += (_, __) => SafeApply();
            filterPanel.Controls.Add(_searchBox);
            fx += 178 + 22;

            // Класс (мультивыбор)
            filterPanel.Controls.Add(MkLbl("Класс:", fx, fy)); fx += 56;
            _classFilter = new MultiDropButton("Все классы", 142);
            _classFilter.Location = new Point(fx, fy - 2);
            _classFilter.SelectionChanged += (_, __) => SafeApply();
            filterPanel.Controls.Add(_classFilter);
            fx += 142 + 22;

            // Школа (мультивыбор)
            filterPanel.Controls.Add(MkLbl("Школа:", fx, fy)); fx += 56;
            _schoolFilter = new MultiDropButton("Все школы", 132);
            _schoolFilter.Location = new Point(fx, fy - 2);
            _schoolFilter.SelectionChanged += (_, __) => SafeApply();
            filterPanel.Controls.Add(_schoolFilter);
            fx += 132 + 22;

            // Уровень (мультивыбор)
            filterPanel.Controls.Add(MkLbl("Уровень:", fx, fy)); fx += 70;
            _levelFilter = new MultiDropButton("Все уровни", 98);
            _levelFilter.Location = new Point(fx, fy - 2);
            _levelFilter.SelectionChanged += (_, __) => SafeApply();
            filterPanel.Controls.Add(_levelFilter);
            fx += 98 + 22;

            // Источник
            filterPanel.Controls.Add(MkLbl("Ист.:", fx, fy)); fx += 44;
            _sourceFilter = MkCombo(fx, fy - 2, 72, "Все");
            filterPanel.Controls.Add(_sourceFilter);
            fx += 72 + 22;

            // Дистанция
            filterPanel.Controls.Add(MkLbl("Дист.:", fx, fy)); fx += 50;
            _rangeFilter = MkCombo(fx, fy - 2, 116, "Все дистанции");
            filterPanel.Controls.Add(_rangeFilter);

            // ── Нижняя панель кнопок ──────────────────────────────────────────────
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White
            };

            var btnOk = new Button
            {
                Text = "Добавить выбранные", Location = new Point(12, 10),
                Size = new Size(196, 32), DialogResult = DialogResult.OK
            };
            btnOk.Click += (_, __) => { SelectedSpellIds = _selected.ToList(); };

            var btnClear = new Button
            {
                Text = "Снять все", Location = new Point(218, 10), Size = new Size(110, 32)
            };
            btnClear.Click += (_, __) => { _selected.Clear(); _canvas.Invalidate(); };

            var btnCancel = new Button
            {
                Text = "Отмена", Location = new Point(338, 10),
                Size = new Size(100, 32), DialogResult = DialogResult.Cancel
            };

            _countLabel = new Label
            {
                AutoSize = false, Size = new Size(200, 22),
                TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.Gray
            };
            bottomPanel.SizeChanged += (_, __) =>
                _countLabel.Location = new Point(bottomPanel.Width - 210, 14);

            bottomPanel.Controls.AddRange(new Control[] { btnOk, btnClear, btnCancel, _countLabel });

            // ── Виртуальный список ────────────────────────────────────────────────
            var listContainer = new Panel { Dock = DockStyle.Fill };

            _vbar = new VScrollBar { Dock = DockStyle.Right };
            _canvas = DB(new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke });

            _canvas.Paint       += (_, pe) => PaintCards(pe.Graphics);
            _canvas.MouseDown   += OnCanvasMouseDown;
            _canvas.MouseWheel  += OnCanvasMouseWheel;
            _canvas.MouseEnter  += (_, __) => _canvas.Focus();   // колёсико без клика
            _canvas.SizeChanged += (_, __) => { UpdateVBarRange(); _canvas.Invalidate(); };

            _vbar.Scroll += (_, se) => { _scrollY = se.NewValue; _canvas.Invalidate(); };

            listContainer.Controls.Add(_canvas);
            listContainer.Controls.Add(_vbar);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(listContainer);
            Controls.Add(filterPanel);
            Controls.Add(bottomPanel);

            // Регистрируем всплывающие панели фильтров на форме (поверх всего)
            _classFilter.RegisterPopupOn(this);
            _schoolFilter.RegisterPopupOn(this);
            _levelFilter.RegisterPopupOn(this);
        }

        // ─── Вспомогательные хелперы ─────────────────────────────────────────────
        private static Label MkLbl(string text, int x, int y) =>
            new Label { Text = text, Location = new Point(x, y), AutoSize = true };

        private ComboBox MkCombo(int x, int y, int w, string allText)
        {
            var cb = new ComboBox
            {
                Location = new Point(x, y), Size = new Size(w, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cb.Items.Add(allText);
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += (_, __) => SafeApply();
            return cb;
        }

        private static T DB<T>(T ctrl) where T : Control
        {
            typeof(Control)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, true);
            return ctrl;
        }

        // ─── Заполнение фильтров из БД ────────────────────────────────────────────
        private void PopulateFilters()
        {
            // Классы — из таблицы Classes
            using (var db = new DDInformationContext())
            {
                var classNames = db.Classes.Select(c => c.Name)
                    .Where(n => n != null).AsEnumerable()
                    .Select(n => n!.Split(':')[0].Trim())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n).ToList();
                _classFilter.Populate(classNames);
            }

            // Школы — уникальные значения из заклинаний
            _schoolFilter.Populate(
                _allSpells.Select(s => CleanField(s.School))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s));

            // Уровни — фиксированный список
            _levelFilter.Populate(new[] { "Заговор", "1", "2", "3", "4", "5", "6", "7", "8", "9" });

            // Источники (одиночный выбор)
            foreach (var s in _allSpells.Select(s => s.Source ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
                _sourceFilter.Items.Add(s);

            // Дистанции (одиночный выбор)
            foreach (var s in _allSpells.Select(s => CleanField(s.Range ?? ""))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
                _rangeFilter.Items.Add(s);
        }

        // ─── Применение фильтров ──────────────────────────────────────────────────
        private void SafeApply()
        {
            try { ApplyFilters(); }
            catch (Exception ex) { Console.WriteLine($"[SpellPicker] {ex.Message}"); }
        }

        private void ApplyFilters()
        {
            if (_canvas == null) return;

            string  search  = _searchBox?.Text ?? "";
            var     classes = _classFilter?.SelectedValues  ?? new List<string>();
            var     schools = _schoolFilter?.SelectedValues ?? new List<string>();
            var     levels  = _levelFilter?.SelectedValues  ?? new List<string>();
            string? source  = (_sourceFilter?.SelectedIndex > 0) ? _sourceFilter.SelectedItem?.ToString() : null;
            string? range   = (_rangeFilter?.SelectedIndex  > 0) ? _rangeFilter.SelectedItem?.ToString()  : null;

            var filtered = _allSpells
                .Where(sp =>
                    (string.IsNullOrEmpty(search) ||
                        (sp.Name ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (classes.Count == 0 ||
                        classes.Any(cls => (sp.Peculiarities ?? "").IndexOf(cls, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                    (schools.Count == 0 ||
                        schools.Any(sch => CleanField(sp.School).Equals(sch, StringComparison.OrdinalIgnoreCase))) &&
                    (levels.Count == 0 ||
                        levels.Any(lvl => LevelMatches(sp.CellLevel, lvl))) &&
                    (source == null ||
                        (sp.Source ?? "").Equals(source, StringComparison.OrdinalIgnoreCase)) &&
                    (range == null ||
                        CleanField(sp.Range ?? "").Equals(range, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(sp => sp.CellLevel).ThenBy(sp => sp.Name ?? "")
                .ToList();

            RebuildList(filtered);
        }

        private static bool LevelMatches(int cellLevel, string filterText) =>
            filterText == "Заговор"
                ? cellLevel == 0
                : int.TryParse(filterText, out int n) && cellLevel == n;

        // ─── Перестройка виртуального списка ─────────────────────────────────────
        private void RebuildList(List<Spell> spells)
        {
            _filteredSpells = spells;
            _scrollY        = 0;
            if (_vbar.Value != 0)
            {
                try { _vbar.Value = 0; } catch { /* bounds not set yet */ }
            }
            UpdateVBarRange();
            _canvas.Invalidate();
            if (_countLabel != null)
                _countLabel.Text = $"Показано: {spells.Count} / {_allSpells.Count}";
        }

        private void UpdateVBarRange()
        {
            if (_canvas == null || _vbar == null) return;
            int totalH    = PadL + _filteredSpells.Count * (CardH + CardGap) + PadL;
            int visibleH  = Math.Max(1, _canvas.ClientSize.Height);
            int maxScroll = Math.Max(0, totalH - visibleH);

            _vbar.LargeChange = visibleH;
            _vbar.Maximum     = maxScroll + visibleH - 1;   // WinForms: max reachable = Max-Large+1
            _vbar.Value       = Math.Min(_scrollY, maxScroll);
            _scrollY          = _vbar.Value;
            _vbar.Enabled     = maxScroll > 0;
        }

        // ─── Виртуальная отрисовка карточек ──────────────────────────────────────
        private void PaintCards(Graphics g)
        {
            g.Clear(Color.WhiteSmoke);
            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (_filteredSpells.Count == 0)
            {
                using var fE = new Font("Arial", 11);
                g.DrawString("Заклинания не найдены", fE, Brushes.Gray, new PointF(20, 20));
                return;
            }

            int cardW = Math.Max(400, _canvas.ClientSize.Width - PadL * 2);
            int step  = CardH + CardGap;
            int first = Math.Max(0, (_scrollY - PadL) / step);
            int last  = Math.Min(_filteredSpells.Count - 1,
                                 (_scrollY + _canvas.ClientSize.Height) / step + 1);

            for (int i = first; i <= last; i++)
            {
                int y  = PadL + i * step - _scrollY;
                var st = g.Save();
                // Клиппинг по области карточки — иначе FillRectangle и DrawLine
                // могут вылезти за пределы текущей карточки
                g.SetClip(new Rectangle(PadL, y, cardW, CardH));
                g.TranslateTransform(PadL, y);
                DrawCard(g, _filteredSpells[i], cardW, CardH,
                         _selected.Contains(_filteredSpells[i].IdSpell));
                g.Restore(st);
            }
        }

        // ─── Обработчики мыши ────────────────────────────────────────────────────
        private void OnCanvasMouseDown(object? sender, MouseEventArgs me)
        {
            // Закрываем открытые выпадающие списки
            _classFilter.HidePopup();
            _schoolFilter.HidePopup();
            _levelFilter.HidePopup();

            if (me.Button != MouseButtons.Left) return;

            int step  = CardH + CardGap;
            int virtY = me.Y + _scrollY - PadL;
            if (virtY < 0) return;

            int idx = virtY / step;
            if (idx < 0 || idx >= _filteredSpells.Count) return;

            // Убеждаемся, что кликнули в тело карточки (не в зазор)
            int cardTop = PadL + idx * step - _scrollY;
            if (me.Y < cardTop || me.Y >= cardTop + CardH) return;

            var spell = _filteredSpells[idx];
            if (_selected.Contains(spell.IdSpell)) _selected.Remove(spell.IdSpell);
            else                                    _selected.Add(spell.IdSpell);

            // Перерисовываем только ту карточку, что изменилась
            _canvas.Invalidate(new Rectangle(PadL, cardTop, _canvas.Width - PadL * 2, CardH));
        }

        private void OnCanvasMouseWheel(object? sender, MouseEventArgs me)
        {
            int maxVal = Math.Max(0, _vbar.Maximum - _vbar.LargeChange + 1);
            int newVal = Math.Max(0, Math.Min(maxVal, _vbar.Value - me.Delta / 3));
            _vbar.Value = newVal;
            _scrollY    = newVal;
            _canvas.Invalidate();
        }

        // ─── Отрисовка одной карточки (GDI+) ─────────────────────────────────────
        private static void DrawCard(Graphics g, Spell spell, int W, int H, bool selected)
        {
            using var sfCenter = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // ── Фон + рамка ──────────────────────────────────────────────────────
            // Используем FillRectangle вместо g.Clear() — Clear() стирает весь канвас,
            // что при виртуальном рендеринге с TranslateTransform уничтожает соседние карточки.
            using (var bgBrush = new SolidBrush(selected ? Color.FromArgb(232, 242, 255) : Color.White))
                g.FillRectangle(bgBrush, 0, 0, W, H);
            using (var borderPen = new Pen(
                selected ? Color.FromArgb(80, 120, 200) : Color.FromArgb(190, 190, 190), 1f))
                g.DrawRectangle(borderPen, 0, 0, W - 1, H - 1);

            // ── Бейдж уровня ─────────────────────────────────────────────────────
            const int lvlL = 10, lvlT = 10, lvlW = 32, lvlH = 22;
            using (var lvlPen = new Pen(Color.Black, 1.5f))
                g.DrawRectangle(lvlPen, lvlL, lvlT, lvlW, lvlH);
            using (var fLvl = new Font("Arial", 9, FontStyle.Bold))
                g.DrawString(spell.CellLevel == 0 ? "З" : spell.CellLevel.ToString(),
                    fLvl, Brushes.Black, new RectangleF(lvlL, lvlT, lvlW, lvlH), sfCenter);

            // ── Название заклинания ───────────────────────────────────────────────
            const int nameL = 52, nameT = 10;
            using (var fName = new Font("Arial", 10, FontStyle.Bold))
            using (var sfNoWrap = new StringFormat
                { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                float nameW = Math.Max(80, W - nameL - 370f);
                g.DrawString(spell.Name ?? "", fName, Brushes.Black,
                    new RectangleF(nameL, nameT, nameW, 22), sfNoWrap);
            }

            // ── Действие / дальность / длительность ───────────────────────────────
            using (var fInfo = new Font("Arial", 8.5f))
            using (var sfNoWrap = new StringFormat
                { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                float ix = Math.Max(200, W - 360f);
                g.DrawString($"⌛ {CleanField(spell.Time     ?? "—")}", fInfo, Brushes.DimGray, new RectangleF(ix,       10, 115, 18), sfNoWrap);
                g.DrawString($"⊞ {CleanField(spell.Range    ?? "—")}", fInfo, Brushes.DimGray, new RectangleF(ix + 120, 10, 115, 18), sfNoWrap);
                g.DrawString($"⏱ {CleanField(spell.Duration ?? "—")}", fInfo, Brushes.DimGray, new RectangleF(ix + 240, 10, 115, 18), sfNoWrap);
            }

            // ── Чекбокс ───────────────────────────────────────────────────────────
            int cbL = W - 30, cbT = 8;
            using (var cbPen = new Pen(
                selected ? Color.FromArgb(30, 100, 200) : Color.FromArgb(140, 140, 140), 1.5f))
                g.DrawRectangle(cbPen, cbL, cbT, 22, 22);
            if (selected)
            {
                using var tick = new Pen(Color.FromArgb(30, 100, 200), 2.5f)
                    { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
                g.DrawLines(tick, new PointF[]
                {
                    new(cbL + 3,  cbT + 11),
                    new(cbL + 9,  cbT + 17),
                    new(cbL + 19, cbT + 5)
                });
            }

            // ── Разделитель 1 ─────────────────────────────────────────────────────
            using (var sep = new Pen(Color.FromArgb(210, 210, 210), 1f))
                g.DrawLine(sep, 10, 38, W - 10, 38);

            // ── Описание (4 строки) ───────────────────────────────────────────────
            using (var fDesc = new Font("Arial", 8.5f))
            using (var sfDesc = new StringFormat { Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(spell.Description ?? "", fDesc, Brushes.Black,
                    new RectangleF(10, 46, W - 20, 62), sfDesc);

            // ── Разделитель 2 ─────────────────────────────────────────────────────
            using (var sep = new Pen(Color.FromArgb(225, 225, 225), 1f))
                g.DrawLine(sep, 10, 115, W - 10, 115);

            // ── Подвал: школа • компоненты • классы ──────────────────────────────
            using (var fFoot = new Font("Arial", 8f))
            using (var sfNoWrap = new StringFormat
                { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                string footTxt = string.Join("  •  ",
                    new[] { CleanField(spell.School), FormatComponents(spell.Components), TrimClasses(spell.Peculiarities) }
                    .Where(s => !string.IsNullOrEmpty(s)));
                g.DrawString(footTxt, fFoot, Brushes.DimGray, new RectangleF(10, 120, W - 72, 22), sfNoWrap);
            }

            // ── Чип источника ─────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(spell.Source))
            {
                var chipR = new RectangleF(W - 60, 119, 52, 19);
                using (var chipBrush = new SolidBrush(Color.FromArgb(55, 55, 55)))
                    g.FillRectangle(chipBrush, chipR);
                using (var fChip = new Font("Arial", 7f, FontStyle.Bold))
                    g.DrawString(spell.Source, fChip, Brushes.White, chipR, sfCenter);
            }
        }

        // ─── Статические утилиты (используются также в Form4.Spells.cs) ──────────

        internal static string CleanField(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            if (s.Contains("\"rus\""))
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    s, "\"rus\"\\s*:\\s*\"([^\"]+)\"");
                if (m.Success) return m.Groups[1].Value;
            }
            return s.Trim('"').Trim();
        }

        internal static string FormatComponents(string? comp)
        {
            if (string.IsNullOrWhiteSpace(comp)) return "";
            return string.Join(" ",
                comp.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().ToUpper()));
        }

        internal static string TrimClasses(string? peculiarities)
        {
            if (string.IsNullOrWhiteSpace(peculiarities)) return "";
            var cleaned = peculiarities.Replace("\"", "").Trim();
            return cleaned.Length > 50 ? cleaned[..50] + "…" : cleaned;
        }

        // ─── Вспомогательный класс: кнопка с мультивыбором ───────────────────────
        private sealed class MultiDropButton : Button
        {
            private readonly string         _allText;
            private readonly CheckedListBox _popup;
            public  List<string>            SelectedValues { get; private set; } = new();
            public  event EventHandler?     SelectionChanged;

            public MultiDropButton(string allText, int w)
            {
                _allText  = allText;
                Size      = new Size(w, 24);
                TextAlign = ContentAlignment.MiddleLeft;
                FlatStyle = FlatStyle.Standard;
                Text      = allText;

                _popup = new CheckedListBox
                {
                    CheckOnClick   = true,
                    Visible        = false,
                    IntegralHeight = false
                };
                // ItemCheck fires ПЕРЕД изменением состояния — используем BeginInvoke
                _popup.ItemCheck += (_, __) =>
                    BeginInvoke((Action)RefreshState);

                // Скрываем при потере фокуса (с небольшой задержкой для устойчивости)
                _popup.Leave += (_, __) =>
                {
                    var t = new System.Windows.Forms.Timer { Interval = 80 };
                    t.Tick += (__, ___) =>
                    {
                        t.Stop(); t.Dispose();
                        if (!_popup.ContainsFocus && !ContainsFocus)
                            _popup.Visible = false;
                    };
                    t.Start();
                };
            }

            /// <summary>Добавляет всплывающий CLB в Controls формы (поверх всего).</summary>
            public void RegisterPopupOn(Form form)
            {
                if (!form.Controls.Contains(_popup))
                {
                    form.Controls.Add(_popup);
                    _popup.BringToFront();
                }
            }

            public void Populate(IEnumerable<string> items)
            {
                _popup.Items.Clear();
                foreach (var it in items) _popup.Items.Add(it);
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                if (_popup.Visible) { _popup.Visible = false; return; }

                var form = FindForm();
                if (form == null || Parent == null) return;

                // Определяем размер: ItemHeight доступен только после создания Handle
                _popup.CreateControl();
                int itemH  = Math.Max(16, _popup.ItemHeight);
                int popupH = Math.Min(220, _popup.Items.Count * itemH + 6);
                _popup.Size = new Size(Math.Max(Width, 170), popupH);

                // Позиционируем под кнопкой в координатах формы
                var pt = form.PointToClient(Parent.PointToScreen(new Point(Left, Bottom)));
                _popup.Location = pt;
                _popup.BringToFront();
                _popup.Visible = true;
                _popup.Focus();
            }

            public void HidePopup() => _popup.Visible = false;

            public void ClearAll()
            {
                for (int i = 0; i < _popup.Items.Count; i++)
                    _popup.SetItemChecked(i, false);
                SelectedValues = new List<string>();
                Text           = _allText;
            }

            private void RefreshState()
            {
                var vals = new List<string>();
                for (int i = 0; i < _popup.Items.Count; i++)
                    if (_popup.GetItemChecked(i))
                        vals.Add(_popup.Items[i]?.ToString() ?? "");
                SelectedValues = vals;
                Text = vals.Count == 0 ? _allText
                     : vals.Count == 1 ? vals[0]
                     : $"{vals[0]} +{vals.Count - 1}";
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
