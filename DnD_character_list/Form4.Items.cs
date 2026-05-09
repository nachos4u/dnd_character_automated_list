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
        // ─── Item card constants ──────────────────────────────────────────────────
        private const int ItCardW        = SpCardW;      // 384 — same width as spell cards
        private const int ItCardH        = 200;
        private const int ItCardCols     = 3;
        private const int ItCardGapX     = SpCardGapX;  // 14
        private const int ItCardGapY     = 10;
        private const int ItCardsPerPage = ItCardCols * 7; // 3×7 = 21

        // Stored inventories for cascade rebuilds (e.g. when spells are modified)
        private List<ItemInventory> _currentItemInventories = new();

        // Page PictureBoxes (for ComputeTraitBaseY)
        private readonly List<PictureBox> _itemCardPages = new();

        // ─── Загрузка предметов персонажа ─────────────────────────────────────────
        internal void LoadCharacterItems()
        {
            try
            {
                using var db = new DDInformationContext();
                _currentItemInventories = db.ItemInventories
                    .Include(ii => ii.IdItemNavigation)
                    .Where(ii => ii.IdCharacter == DataIDCharacter)
                    .OrderBy(ii => ii.IdItemNavigation.Name)
                    .ToList();
            }
            catch
            {
                _currentItemInventories = new List<ItemInventory>();
            }

            // Fill the summary TextBox on the character sheet
            ItemsTextBox.Text = _currentItemInventories.Any()
                ? string.Join(Environment.NewLine,
                    _currentItemInventories.Select(ii => $"{ii.IdItemNavigation.Name} x{ii.Quantity}"))
                : "";

            RebuildItemCardPages();
        }

        // ─── Перестройка страниц предметов ────────────────────────────────────────
        internal void RebuildItemCardPages()
        {
            this.SuspendLayout();

            // Remove old item card controls
            var toRemove = this.Controls.OfType<Control>()
                .Where(c => c.Tag is string t && t == "itemcard")
                .ToList();
            foreach (var c in toRemove) { this.Controls.Remove(c); c.Dispose(); }
            _itemCardPages.Clear();

            var inventories = _currentItemInventories;

            if (!inventories.Any())
            {
                RebuildTraitCardPages();
                this.ResumeLayout(true);
                return;
            }

            int baseY     = ComputeItemBaseY();
            int pageCount = (inventories.Count + ItCardsPerPage - 1) / ItCardsPerPage;

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
                    Tag         = "itemcard"
                };
                this.Controls.Add(pagePb);
                pagePb.SendToBack();
                _itemCardPages.Add(pagePb);

                // ── Page header ───────────────────────────────────────────────────
                var headerLbl = new Label
                {
                    Text     = pageCount == 1 ? "Инвентарь" : $"Инвентарь (стр. {pageIdx + 1} из {pageCount})",
                    Location = new Point(45 + SpPagePadL, pageY + 10),
                    AutoSize = true,
                    Font     = new Font("Arial", 11, FontStyle.Bold),
                    Tag      = "itemcard"
                };
                this.Controls.Add(headerLbl);
                headerLbl.BringToFront();

                // ── Cards on this page ────────────────────────────────────────────
                var pageItems = inventories
                    .Skip(pageIdx * ItCardsPerPage)
                    .Take(ItCardsPerPage)
                    .ToList();

                for (int i = 0; i < pageItems.Count; i++)
                {
                    int col   = i % ItCardCols;
                    int row   = i / ItCardCols;
                    int cardX = 45 + SpPagePadL + col * (ItCardW + ItCardGapX);
                    int cardY = pageY + SpPagePadH + SpPagePadV + row * (ItCardH + ItCardGapY);

                    var card = BuildItemCard(pageItems[i]);
                    card.Location = new Point(cardX, cardY);
                    card.Tag      = "itemcard";
                    this.Controls.Add(card);
                    card.BringToFront();
                }
            }

            // Extend scroll area to cover item pages
            if (_itemCardPages.Any())
            {
                int needed = _itemCardPages.Last().Bottom + 80;
                this.AutoScrollMinSize = new Size(
                    this.AutoScrollMinSize.Width,
                    Math.Max(this.AutoScrollMinSize.Height, needed));
            }

            // Cascade: reposition trait cards below item cards
            RebuildTraitCardPages();

            this.ResumeLayout(true);
            this.Invalidate(true);
        }

        // Compute the Y coordinate where item pages start (below spell pages)
        private int ComputeItemBaseY()
        {
            if (_spellCardPages.Any())
                return _spellCardPages.Last().Bottom + SpPageGap;
            return _spellCardsBaseY;
        }

        // ─── Построение одной карточки предмета ──────────────────────────────────
        private static Panel BuildItemCard(ItemInventory inv)
        {
            var item = inv.IdItemNavigation;
            var card = new Panel
            {
                Size        = new Size(ItCardW, ItCardH),
                BackColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // ── Name + quantity badge ─────────────────────────────────────────────
            var nameLbl = new Label
            {
                Text         = item.Name,
                Location     = new Point(8, 6),
                Size         = new Size(ItCardW - 60, 22),
                Font         = new Font("Arial", 10, FontStyle.Bold),
                AutoEllipsis = true
            };

            var qtyBadge = new Label
            {
                Text      = $"×{inv.Quantity}",
                Location  = new Point(ItCardW - 52, 6),
                Size      = new Size(44, 20),
                Font      = new Font("Arial", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 100, 160),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Separator 1 ───────────────────────────────────────────────────────
            var sep1 = new Panel { Location = new Point(8, 32), Size = new Size(ItCardW - 18, 1), BackColor = Color.Black };

            // ── Meta line: type | price | weight ─────────────────────────────────
            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.ItemType)) metaParts.Add(item.ItemType);
            if (!string.IsNullOrWhiteSpace(item.Price))    metaParts.Add($"Цена: {item.Price}");
            if (item.Weight.HasValue)                       metaParts.Add($"Вес: {item.Weight:0.##} фн.");

            var metaLbl = new Label
            {
                Text         = metaParts.Count > 0 ? string.Join("  |  ", metaParts) : "",
                Location     = new Point(8, 36),
                Size         = new Size(ItCardW - 16, 16),
                Font         = new Font("Arial", 7.5f),
                ForeColor    = Color.DimGray,
                AutoEllipsis = true
            };

            // ── Rarity ────────────────────────────────────────────────────────────
            var rarity     = item.Rarity ?? (item.IsMagic ? "редкость не определена" : "не магические");
            var rarityLbl  = new Label
            {
                Text         = rarity,
                Location     = new Point(8, 54),
                Size         = new Size(ItCardW - 16, 14),
                Font         = new Font("Arial", 7.5f, FontStyle.Italic),
                ForeColor    = GetItemRarityColor(rarity),
                AutoEllipsis = true
            };

            // ── Separator 2 ───────────────────────────────────────────────────────
            var sep2 = new Panel { Location = new Point(8, 70), Size = new Size(ItCardW - 18, 1), BackColor = Color.LightGray };

            // ── Description ───────────────────────────────────────────────────────
            var descLbl = new Label
            {
                Text     = item.Description ?? "",
                Location = new Point(8, 74),
                Size     = new Size(ItCardW - 16, ItCardH - 74 - 26),
                Font     = new Font("Arial", 8),
                AutoSize = false,
                AutoEllipsis = false
            };

            // ── Separator 3 ───────────────────────────────────────────────────────
            var sep3 = new Panel { Location = new Point(8, ItCardH - 24), Size = new Size(ItCardW - 18, 1), BackColor = Color.LightGray };

            // ── Source chip ───────────────────────────────────────────────────────
            var srcChip = new Label
            {
                Text      = item.Source ?? "",
                Location  = new Point(ItCardW - 50, ItCardH - 20),
                Size      = new Size(42, 18),
                Font      = new Font("Arial", 7f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.AddRange(new Control[] { nameLbl, qtyBadge, sep1, metaLbl, rarityLbl, sep2, descLbl, sep3, srcChip });
            return card;
        }

        private static Color GetItemRarityColor(string? rarity) => rarity?.ToLower() switch
        {
            "обычный"              => Color.Gray,
            "необычный"            => Color.ForestGreen,
            "редкое"               => Color.RoyalBlue,
            "очень редкий"         => Color.Purple,
            "легендарное"          => Color.OrangeRed,
            "артефакт"             => Color.Crimson,
            "редкость варьируется" => Color.Teal,
            "не магические"        => Color.DimGray,
            _                      => Color.DimGray
        };

        // ─── Кнопка "+" рядом с инвентарём ───────────────────────────────────────
        private void InventoryAddButton_Click(object sender, EventArgs e)
        {
            // ── 1. Load existing inventory (materialized before context disposal) ──
            List<(int id, int qty)> existing = new();
            try
            {
                using var db = new DDInformationContext();
                existing = db.ItemInventories
                    .Where(ii => ii.IdCharacter == DataIDCharacter)
                    .Select(ii => new { ii.IdItem, ii.Quantity })
                    .ToList()
                    .Select(x => (x.IdItem, x.Quantity))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки инвентаря:\n{ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // ── 2. Show picker ────────────────────────────────────────────────────
            Dictionary<int, int> chosen;
            try
            {
                using var picker = new ItemPickerForm(existing);
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                chosen = picker.SelectedItemQuantities;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при открытии окна выбора предметов:\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (chosen.Count == 0) return;

            // ── 3. Save chosen items ──────────────────────────────────────────────
            try
            {
                using var db = new DDInformationContext();
                foreach (var (itemId, qty) in chosen)
                {
                    var inv = db.ItemInventories
                        .FirstOrDefault(ii => ii.IdCharacter == DataIDCharacter && ii.IdItem == itemId);
                    if (inv != null)
                        inv.Quantity = qty;
                    else
                        db.ItemInventories.Add(new ItemInventory
                        {
                            IdCharacter = DataIDCharacter,
                            IdItem      = itemId,
                            Quantity    = qty
                        });
                }
                db.SaveChanges();
                LoadCharacterItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения инвентаря:\n{ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
