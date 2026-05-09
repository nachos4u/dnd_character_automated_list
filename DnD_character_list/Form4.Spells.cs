using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Константы карточек заклинания ────────────────────────────────────────
        private const int SpCardW        = 384;
        private const int SpCardH        = 540;
        private const int SpCardCols     = 3;
        private const int SpCardRows     = 3;
        private const int SpCardsPerPage = SpCardCols * SpCardRows; // 9
        private const int SpCardGapX     = 14;
        private const int SpCardGapY     = 14;
        private const int SpPagePadH     = 42;   // высота заголовка страницы
        private const int SpPagePadV     = 20;   // вертикальный отступ после заголовка и снизу
        private const int SpPagePadL     = 29;   // горизонтальный отступ слева
        private const int SpPageW        = 1240;
        private const int SpPageH        = 1754;
        private const int SpPageGap      = 50;   // зазор между страницами

        // Доступная высота описания (с учётом шапки карточки и подвала)
        // Первая карточка: от y=80 до y=SpCardH-26 → 540-26-80 = 434, минус небольшой запас
        private const int DescHFirst = 422;
        // Карточка-продолжение: от y=40 до y=SpCardH-26 → 540-26-40 = 474, минус запас
        private const int DescHCont  = 462;

        // Y-координата начала первой страницы заклинаний (задаётся из конструктора)
        private int _spellCardsBaseY;

        // Список PictureBox-страниц заклинаний (фоновые «листы»)
        private readonly List<PictureBox> _spellCardPages = new();

        // ─── Вспомогательная структура — один слот карточки ──────────────────────
        private struct CardSlot
        {
            public Spell  Spell;
            public string DescText;
            public bool   IsContinuation;
        }

        // ─── Инициализация ────────────────────────────────────────────────────────
        /// <summary>Задаёт базовую Y-координату для страниц заклинаний.</summary>
        public void InitSpellCardSection(int baseY) => _spellCardsBaseY = baseY;

        // ─── Загрузка заклинаний персонажа ────────────────────────────────────────
        public void LoadCharacterSpells()
        {
            using var db = new DDInformationContext();
            var character = db.Characters
                .Include(c => c.IdSpells)
                .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

            if (character == null) return;

            var spells = character.IdSpells
                .OrderBy(s => s.CellLevel).ThenBy(s => s.Name)
                .ToList();

            // Обновляем список без триггера автосохранения
            isLoading = true;
            try
            {
                SpellsTextBox.Text = string.Join("\r\n",
                    spells.Select(s =>
                    {
                        string lvl = s.CellLevel == 0 ? "Заговор" : $"Ур. {s.CellLevel}";
                        return $"[{lvl}] {s.Name}";
                    }));
            }
            finally
            {
                isLoading = false;
            }

            // Перестраиваем страницы карточек
            RebuildSpellCardPages(spells);
        }

        // ─── Построение слотов с разбивкой длинных описаний ──────────────────────
        private static List<CardSlot> BuildCardSlots(List<Spell> spells)
        {
            var slots = new List<CardSlot>();
            using var descFont = new Font("Arial", 8f);

            foreach (var spell in spells)
            {
                string remaining = spell.Description?.Trim() ?? "";

                bool first = true;
                do
                {
                    int maxH = first ? DescHFirst : DescHCont;
                    var (fit, rest) = SplitFit(remaining, descFont, SpCardW - 16, maxH);
                    slots.Add(new CardSlot
                    {
                        Spell         = spell,
                        DescText      = fit,
                        IsContinuation = !first
                    });
                    remaining = rest;
                    first = false;
                }
                while (!string.IsNullOrEmpty(remaining));
            }

            return slots;
        }

        // ─── Разбивка текста по высоте (двоичный поиск по словам) ────────────────
        private static (string fit, string rest) SplitFit(
            string text, Font font, int maxWidth, int maxHeight)
        {
            if (string.IsNullOrEmpty(text)) return ("", "");

            var flags = TextFormatFlags.WordBreak;

            // Если весь текст помещается — возвращаем как есть
            var sizeAll = TextRenderer.MeasureText(
                text, font, new Size(maxWidth, int.MaxValue), flags);
            if (sizeAll.Height <= maxHeight)
                return (text, "");

            // Двоичный поиск по количеству слов
            string[] words = text.Split(' ');
            int lo = 1, hi = words.Length - 1, best = 0;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                string candidate = string.Join(" ", words, 0, mid);
                var s = TextRenderer.MeasureText(
                    candidate, font, new Size(maxWidth, int.MaxValue), flags);

                if (s.Height <= maxHeight) { best = mid; lo = mid + 1; }
                else hi = mid - 1;
            }

            if (best <= 0) best = 1; // хотя бы одно слово

            string fitText  = string.Join(" ", words, 0, best);
            string restText = string.Join(" ", words, best, words.Length - best).Trim();
            return (fitText, restText);
        }

        // ─── Перестройка страниц заклинаний ───────────────────────────────────────
        private void RebuildSpellCardPages(List<Spell> spells)
        {
            this.SuspendLayout();

            // Удаляем все старые контролы заклинаний
            var toRemove = this.Controls.OfType<Control>()
                .Where(c => c.Tag is string t && t == "spellcard")
                .ToList();
            foreach (var c in toRemove) { this.Controls.Remove(c); c.Dispose(); }
            _spellCardPages.Clear();

            if (!spells.Any())
            {
                // Cascade: reposition item/trait cards even when spells section is empty
                RebuildItemCardPages();
                this.ResumeLayout(true);
                return;
            }

            // Строим слоты (с разбивкой описания при необходимости)
            var slots     = BuildCardSlots(spells);
            int pageCount = (slots.Count + SpCardsPerPage - 1) / SpCardsPerPage;

            for (int pageIdx = 0; pageIdx < pageCount; pageIdx++)
            {
                int pageY = _spellCardsBaseY + pageIdx * (SpPageH + SpPageGap);

                // ── Фоновый лист ─────────────────────────────────────────────────
                var pagePb = new PictureBox
                {
                    Name        = $"spellCardPage_{pageIdx}",
                    Location    = new Point(45, pageY),
                    Size        = new Size(SpPageW, SpPageH),
                    BackColor   = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    TabStop     = false,
                    Tag         = "spellcard"
                };
                this.Controls.Add(pagePb);
                pagePb.SendToBack();
                _spellCardPages.Add(pagePb);

                // ── Заголовок страницы ───────────────────────────────────────────
                var headerLbl = new Label
                {
                    Text = pageCount == 1
                        ? "Список заклинаний"
                        : $"Список заклинаний (стр. {pageIdx + 1} из {pageCount})",
                    Location  = new Point(45 + SpPagePadL, pageY + 10),
                    AutoSize  = true,
                    Font      = new Font("Arial", 11, FontStyle.Bold),
                    Tag       = "spellcard"
                };
                this.Controls.Add(headerLbl);
                headerLbl.BringToFront();

                // ── Карточки заклинаний на этой странице ─────────────────────────
                var pageSlots = slots
                    .Skip(pageIdx * SpCardsPerPage)
                    .Take(SpCardsPerPage)
                    .ToList();

                for (int i = 0; i < pageSlots.Count; i++)
                {
                    int col   = i % SpCardCols;
                    int row   = i / SpCardCols;
                    int cardX = 45 + SpPagePadL + col * (SpCardW + SpCardGapX);
                    int cardY = pageY + SpPagePadH + SpPagePadV + row * (SpCardH + SpCardGapY);

                    var card = BuildCard(pageSlots[i]);
                    card.Location = new Point(cardX, cardY);
                    card.Tag      = "spellcard";
                    this.Controls.Add(card);
                    card.BringToFront();
                }
            }

            // Расширяем область прокрутки под заклинания
            var last   = _spellCardPages.Last();
            int needed = last.Bottom + 80;
            this.AutoScrollMinSize = new Size(this.AutoScrollMinSize.Width, needed);

            // Cascade: reposition item/trait cards below spell pages
            RebuildItemCardPages();

            this.ResumeLayout(true);
            this.Invalidate(true);

            // Прокручиваем вниз к первой странице карточек
            this.AutoScrollPosition = new Point(0,
                Math.Max(0, _spellCardPages.First().Top - 20));
        }

        // ─── Построение одной карточки заклинания ────────────────────────────────
        private static Panel BuildCard(CardSlot slot)
        {
            var card = new Panel
            {
                Size        = new Size(SpCardW, SpCardH),
                BackColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            if (!slot.IsContinuation)
            {
                // ── Бейдж уровня ─────────────────────────────────────────────────
                var lvlBox = new Panel
                {
                    Location  = new Point(8, 8),
                    Size      = new Size(30, 22),
                    BackColor = Color.White
                };
                var capturedSpell = slot.Spell;
                lvlBox.Paint += (s, pe) =>
                {
                    using var pen = new Pen(Color.Black, 1.5f);
                    pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    pe.Graphics.DrawRectangle(pen, 0, 0, lvlBox.Width - 1, lvlBox.Height - 1);
                    string lvlStr = capturedSpell.CellLevel == 0 ? "З" : capturedSpell.CellLevel.ToString();
                    using var f = new Font("Arial", 9, FontStyle.Bold);
                    pe.Graphics.DrawString(lvlStr, f, Brushes.Black,
                        new RectangleF(0, 0, lvlBox.Width, lvlBox.Height),
                        new StringFormat
                        {
                            Alignment     = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        });
                };

                // ── Название заклинания ───────────────────────────────────────────
                var nameLbl = new Label
                {
                    Text        = slot.Spell.Name,
                    Location    = new Point(46, 8),
                    Size        = new Size(SpCardW - 56, 22),
                    Font        = new Font("Arial", 10, FontStyle.Bold),
                    TextAlign   = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };

                // ── Разделитель 1 (чёрный) ────────────────────────────────────────
                var sep1 = new Panel
                {
                    Location  = new Point(8, 34),
                    Size      = new Size(SpCardW - 18, 1),
                    BackColor = Color.Black
                };

                // ── Действие / дальность / длительность ───────────────────────────
                string timeStr  = SpellPickerForm.CleanField(slot.Spell.Time     ?? "—");
                string rangeStr = SpellPickerForm.CleanField(slot.Spell.Range    ?? "—");
                string durStr   = SpellPickerForm.CleanField(slot.Spell.Duration ?? "—");
                var infoLbl = new Label
                {
                    Text        = $"⌛ {timeStr}   ⊞ {rangeStr}   ⏱ {durStr}",
                    Location    = new Point(8, 38),
                    Size        = new Size(SpCardW - 16, 18),
                    Font        = new Font("Arial", 8),
                    AutoEllipsis = true
                };

                // ── Школа магии (курсив) ──────────────────────────────────────────
                var schoolLbl = new Label
                {
                    Text        = SpellPickerForm.CleanField(slot.Spell.School),
                    Location    = new Point(8, 58),
                    Size        = new Size(SpCardW - 16, 16),
                    Font        = new Font("Arial", 7.5f, FontStyle.Italic),
                    ForeColor   = Color.DimGray,
                    AutoEllipsis = true
                };

                // ── Разделитель 2 (серый) ─────────────────────────────────────────
                var sep2 = new Panel
                {
                    Location  = new Point(8, 76),
                    Size      = new Size(SpCardW - 18, 1),
                    BackColor = Color.LightGray
                };

                // ── Описание ─────────────────────────────────────────────────────
                var descLbl = new Label
                {
                    Text     = slot.DescText,
                    Location = new Point(8, 80),
                    Size     = new Size(SpCardW - 16, DescHFirst),
                    Font     = new Font("Arial", 8),
                    AutoSize = false,
                    AutoEllipsis = false
                };

                // ── Разделитель 3 (серый, перед подвалом) ─────────────────────────
                var sep3 = new Panel
                {
                    Location  = new Point(8, SpCardH - 26),
                    Size      = new Size(SpCardW - 18, 1),
                    BackColor = Color.LightGray
                };

                // ── Компоненты + классы ───────────────────────────────────────────
                var footLbl = new Label
                {
                    Text      = $"{SpellPickerForm.FormatComponents(slot.Spell.Components)}  " +
                                $"{SpellPickerForm.TrimClasses(slot.Spell.Peculiarities)}",
                    Location  = new Point(8, SpCardH - 22),
                    Size      = new Size(SpCardW - 58, 18),
                    Font      = new Font("Arial", 7f),
                    ForeColor = Color.DimGray,
                    AutoEllipsis = true
                };

                // ── Чип источника ─────────────────────────────────────────────────
                var srcChip = new Label
                {
                    Text      = slot.Spell.Source ?? "",
                    Location  = new Point(SpCardW - 50, SpCardH - 22),
                    Size      = new Size(42, 20),
                    Font      = new Font("Arial", 7f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(50, 50, 50),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                card.Controls.AddRange(new Control[]
                {
                    lvlBox, nameLbl, sep1, infoLbl, schoolLbl, sep2, descLbl, sep3, footLbl, srcChip
                });
            }
            else
            {
                // ── Заголовок продолжения ─────────────────────────────────────────
                var contLbl = new Label
                {
                    Text      = $"↩ {slot.Spell.Name} (продолжение)",
                    Location  = new Point(8, 8),
                    Size      = new Size(SpCardW - 16, 24),
                    Font      = new Font("Arial", 9, FontStyle.Italic | FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };

                // ── Разделитель 1 (чёрный) ────────────────────────────────────────
                var sep1 = new Panel
                {
                    Location  = new Point(8, 36),
                    Size      = new Size(SpCardW - 18, 1),
                    BackColor = Color.Black
                };

                // ── Продолжение описания ──────────────────────────────────────────
                var descLbl = new Label
                {
                    Text     = slot.DescText,
                    Location = new Point(8, 40),
                    Size     = new Size(SpCardW - 16, DescHCont),
                    Font     = new Font("Arial", 8),
                    AutoSize = false,
                    AutoEllipsis = false
                };

                // ── Разделитель 2 (серый, перед подвалом) ─────────────────────────
                var sep2 = new Panel
                {
                    Location  = new Point(8, SpCardH - 26),
                    Size      = new Size(SpCardW - 18, 1),
                    BackColor = Color.LightGray
                };

                // ── Компоненты + классы ───────────────────────────────────────────
                var footLbl = new Label
                {
                    Text      = $"{SpellPickerForm.FormatComponents(slot.Spell.Components)}  " +
                                $"{SpellPickerForm.TrimClasses(slot.Spell.Peculiarities)}",
                    Location  = new Point(8, SpCardH - 22),
                    Size      = new Size(SpCardW - 58, 18),
                    Font      = new Font("Arial", 7f),
                    ForeColor = Color.DimGray,
                    AutoEllipsis = true
                };

                // ── Чип источника ─────────────────────────────────────────────────
                var srcChip = new Label
                {
                    Text      = slot.Spell.Source ?? "",
                    Location  = new Point(SpCardW - 50, SpCardH - 22),
                    Size      = new Size(42, 20),
                    Font      = new Font("Arial", 7f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(50, 50, 50),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                card.Controls.AddRange(new Control[] { contLbl, sep1, descLbl, sep2, footLbl, srcChip });
            }

            return card;
        }

        // ─── Открытие пикера заклинаний ───────────────────────────────────────────
        private void NewSpellButton_Click(object sender, EventArgs e)
        {
            // Получаем текущий список заклинаний персонажа
            List<int> currentIds;
            using (var db = new DDInformationContext())
            {
                var ch = db.Characters
                    .Include(c => c.IdSpells)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                currentIds = ch?.IdSpells.Select(s => s.IdSpell).ToList() ?? new List<int>();
            }

            using var picker = new SpellPickerForm(currentIds);
            if (picker.ShowDialog(this) != DialogResult.OK) return;

            // Сохраняем выбор в БД
            try
            {
                using var db = new DDInformationContext();
                var ch = db.Characters
                    .Include(c => c.IdSpells)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                if (ch == null) return;

                var pickedSpells = db.Spells
                    .Where(s => picker.SelectedSpellIds.Contains(s.IdSpell))
                    .ToList();

                ch.IdSpells.Clear();
                foreach (var s in pickedSpells)
                    ch.IdSpells.Add(s);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения заклинаний:\n{ex.Message}", "Ошибка");
                return;
            }

            // Перезагружаем страницу заклинаний
            LoadCharacterSpells();
        }
    }
}
