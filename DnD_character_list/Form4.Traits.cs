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
        // ─── Trait card constants ─────────────────────────────────────────────────
        private const int TrCardW        = SpCardW;      // 384
        private const int TrCardH        = 240;
        private const int TrCardCols     = 3;
        private const int TrCardGapX     = SpCardGapX;  // 14
        private const int TrCardGapY     = 10;
        private const int TrCardsPerPage = TrCardCols * 6; // 3×6 = 18

        // Stored traits for cascade rebuilds
        private List<Trait> _currentTraits = new();

        // Page PictureBoxes
        private readonly List<PictureBox> _traitCardPages = new();

        // ─── Загрузка черт персонажа ──────────────────────────────────────────────
        internal void LoadCharacterTraits()
        {
            try
            {
                using var db = new DDInformationContext();
                var character = db.Characters
                    .Include(c => c.IdTraits)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);

                _currentTraits = character?.IdTraits
                    .OrderBy(t => t.Name ?? t.CharTics)
                    .ToList() ?? new List<Trait>();
            }
            catch
            {
                _currentTraits = new List<Trait>();
            }

            // Fill summary TextBox on character sheet
            TraitsTextBox.Text = _currentTraits.Any()
                ? string.Join(Environment.NewLine,
                    _currentTraits.Select(t => t.Name ?? t.CharTics ?? "(без названия)"))
                : "";

            RebuildTraitCardPages();
        }

        // ─── Перестройка страниц черт ─────────────────────────────────────────────
        internal void RebuildTraitCardPages()
        {
            this.SuspendLayout();

            // Remove old trait card controls
            var toRemove = this.Controls.OfType<Control>()
                .Where(c => c.Tag is string t && t == "traitcard")
                .ToList();
            foreach (var c in toRemove) { this.Controls.Remove(c); c.Dispose(); }
            _traitCardPages.Clear();

            var traits = _currentTraits;

            if (!traits.Any())
            {
                this.ResumeLayout(true);
                return;
            }

            int baseY     = ComputeTraitBaseY();
            int pageCount = (traits.Count + TrCardsPerPage - 1) / TrCardsPerPage;

            for (int pageIdx = 0; pageIdx < pageCount; pageIdx++)
            {
                int pageY = baseY + pageIdx * (SpPageH + SpPageGap);

                // ── Page background ───────────────────────────────────────────────
                var pagePb = new PictureBox
                {
                    Location    = new Point(45, pageY),
                    Size        = new Size(SpPageW, SpPageH),
                    BackColor   = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    TabStop     = false,
                    Tag         = "traitcard"
                };
                this.Controls.Add(pagePb);
                pagePb.SendToBack();
                _traitCardPages.Add(pagePb);

                // ── Page header ───────────────────────────────────────────────────
                var headerLbl = new Label
                {
                    Text     = pageCount == 1 ? "Черты" : $"Черты (стр. {pageIdx + 1} из {pageCount})",
                    Location = new Point(45 + SpPagePadL, pageY + 10),
                    AutoSize = true,
                    Font     = new Font("Arial", 11, FontStyle.Bold),
                    Tag      = "traitcard"
                };
                this.Controls.Add(headerLbl);
                headerLbl.BringToFront();

                // ── Cards on this page ────────────────────────────────────────────
                var pageTraits = traits
                    .Skip(pageIdx * TrCardsPerPage)
                    .Take(TrCardsPerPage)
                    .ToList();

                for (int i = 0; i < pageTraits.Count; i++)
                {
                    int col   = i % TrCardCols;
                    int row   = i / TrCardCols;
                    int cardX = 45 + SpPagePadL + col * (TrCardW + TrCardGapX);
                    int cardY = pageY + SpPagePadH + SpPagePadV + row * (TrCardH + TrCardGapY);

                    var card = BuildTraitCard(pageTraits[i]);
                    card.Location = new Point(cardX, cardY);
                    card.Tag      = "traitcard";
                    this.Controls.Add(card);
                    card.BringToFront();
                }
            }

            // Extend scroll area to cover trait pages
            if (_traitCardPages.Any())
            {
                int needed = _traitCardPages.Last().Bottom + 80;
                this.AutoScrollMinSize = new Size(
                    this.AutoScrollMinSize.Width,
                    Math.Max(this.AutoScrollMinSize.Height, needed));
            }

            this.ResumeLayout(true);
            this.Invalidate(true);
        }

        // Compute Y where trait pages start (below item pages, or spell pages, or baseY)
        private int ComputeTraitBaseY()
        {
            if (_itemCardPages.Any())
                return _itemCardPages.Last().Bottom + SpPageGap;
            if (_spellCardPages.Any())
                return _spellCardPages.Last().Bottom + SpPageGap;
            return _spellCardsBaseY;
        }

        // ─── Построение одной карточки черты ─────────────────────────────────────
        private static Panel BuildTraitCard(Trait trait)
        {
            var card = new Panel
            {
                Size        = new Size(TrCardW, TrCardH),
                BackColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            string displayName = trait.Name ?? trait.CharTics ?? "(без названия)";

            // ── Name ─────────────────────────────────────────────────────────────
            var nameLbl = new Label
            {
                Text         = displayName,
                Location     = new Point(8, 6),
                Size         = new Size(TrCardW - 60, 22),
                Font         = new Font("Arial", 10, FontStyle.Bold),
                AutoEllipsis = true
            };

            // ── Source chip ───────────────────────────────────────────────────────
            var srcChip = new Label
            {
                Text      = trait.Source ?? "",
                Location  = new Point(TrCardW - 50, 6),
                Size      = new Size(42, 20),
                Font      = new Font("Arial", 7f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Separator 1 ───────────────────────────────────────────────────────
            var sep1 = new Panel { Location = new Point(8, 32), Size = new Size(TrCardW - 18, 1), BackColor = Color.Black };

            // ── Requirements (if any) ─────────────────────────────────────────────
            int descStartY = 36;
            var controls   = new List<Control> { nameLbl, srcChip, sep1 };

            if (!string.IsNullOrWhiteSpace(trait.Requirements))
            {
                var reqLbl = new Label
                {
                    Text         = $"Требования: {trait.Requirements}",
                    Location     = new Point(8, 36),
                    Size         = new Size(TrCardW - 16, 16),
                    Font         = new Font("Arial", 8f, FontStyle.Italic),
                    ForeColor    = Color.FromArgb(140, 80, 10),
                    AutoEllipsis = true
                };

                var sep2 = new Panel { Location = new Point(8, 54), Size = new Size(TrCardW - 18, 1), BackColor = Color.LightGray };

                controls.Add(reqLbl);
                controls.Add(sep2);
                descStartY = 58;
            }

            // ── Description ───────────────────────────────────────────────────────
            var descLbl = new Label
            {
                Text     = trait.Description ?? "",
                Location = new Point(8, descStartY),
                Size     = new Size(TrCardW - 16, TrCardH - descStartY - 26),
                Font     = new Font("Arial", 8),
                AutoSize = false,
                AutoEllipsis = false
            };
            controls.Add(descLbl);

            // ── Separator 3 ───────────────────────────────────────────────────────
            var sep3 = new Panel { Location = new Point(8, TrCardH - 24), Size = new Size(TrCardW - 18, 1), BackColor = Color.LightGray };
            controls.Add(sep3);

            card.Controls.AddRange(controls.ToArray());
            return card;
        }

        // ─── Кнопка "+" рядом с чертами ──────────────────────────────────────────
        private void TraitAddButton_Click(object sender, EventArgs e)
        {
            var currentIds = Array.Empty<int>().AsEnumerable();
            try
            {
                using var db = new DDInformationContext();
                var character = db.Characters
                    .Include(c => c.IdTraits)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                if (character != null)
                    currentIds = character.IdTraits.Select(t => t.IdTrait);
            }
            catch { }

            using var picker = new TraitPickerForm(currentIds);
            if (picker.ShowDialog(this) != DialogResult.OK) return;

            var chosenIds = picker.SelectedTraitIds;

            try
            {
                using var db = new DDInformationContext();
                var character = db.Characters
                    .Include(c => c.IdTraits)
                    .FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                if (character == null) return;

                // Remove deselected
                var toRemove = character.IdTraits
                    .Where(t => !chosenIds.Contains(t.IdTrait))
                    .ToList();
                foreach (var t in toRemove)
                    character.IdTraits.Remove(t);

                // Add newly selected
                var existingIds = character.IdTraits.Select(t => t.IdTrait).ToHashSet();
                var toAddIds    = chosenIds.Where(id => !existingIds.Contains(id)).ToList();
                if (toAddIds.Any())
                {
                    var newTraits = db.Traits.Where(t => toAddIds.Contains(t.IdTrait)).ToList();
                    foreach (var t in newTraits)
                        character.IdTraits.Add(t);
                }

                db.SaveChanges();
                LoadCharacterTraits();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения черт:\n{ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
