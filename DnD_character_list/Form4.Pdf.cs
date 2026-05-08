using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Экспорт в PDF ─────────────────────────────────────────────────────────
        private void ExportPdfButton_Click(object sender, EventArgs e)
        {
            // Находим все страницы — это PictureBox-ы, кроме pictureBox2 (кнопка «назад»)
            var pages = this.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.Name != "pictureBox2")
                .OrderBy(pb => pb.Location.Y)
                .ToList();

            if (!pages.Any())
            {
                MessageBox.Show("Нет страниц для экспорта.", "PDF");
                return;
            }

            using var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = false;
            pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);

            // Пробуем найти принтер с «PDF» в названии (Microsoft Print to PDF и др.)
            string pdfPrinter = null;
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    pdfPrinter = printer;
                    break;
                }
            }

            if (pdfPrinter != null)
            {
                // Автоматический режим: сразу выбираем PDF-принтер и просим путь файла
                pd.PrinterSettings.PrinterName = pdfPrinter;
                pd.PrinterSettings.PrintToFile = true;

                using var saveDlg = new SaveFileDialog
                {
                    Filter    = "PDF файлы (*.pdf)|*.pdf",
                    FileName  = $"character_{DataIDCharacter}.pdf",
                    Title     = "Сохранить лист персонажа как PDF"
                };
                if (saveDlg.ShowDialog() != DialogResult.OK) return;
                pd.PrinterSettings.PrintFileName = saveDlg.FileName;
            }
            else
            {
                // PDF-принтер не найден → показываем диалог выбора принтера
                using var printDlg = new PrintDialog { Document = pd };
                if (printDlg.ShowDialog() != DialogResult.OK) return;
            }

            // Настраиваем размер страницы под размер пикчербокса (1240 × 1754 px → 96 dpi)
            int pageW = 827;
            int pageH = 1169;
            pd.DefaultPageSettings.PaperSize = new PaperSize("CharSheet",
                (int)(pageW / 96.0 * 100), (int)(pageH / 96.0 * 100)); // в 1/100 дюйма

            // ── Печать постранично ────────────────────────────────────────────────
            int pageIndex = 0;
            pd.PrintPage += (s, pe) =>
            {
                var pb = pages[pageIndex];
                using var bmp = RenderPageRegion(pb.Location.X, pb.Location.Y, pb.Width, pb.Height);

                // Вписываем изображение в область печати без полей
                RectangleF bounds = pe.PageBounds;
                float scaleX = bounds.Width  / bmp.Width;
                float scaleY = bounds.Height / bmp.Height;
                float scale  = Math.Min(scaleX, scaleY);

                float drawW = bmp.Width  * scale;
                float drawH = bmp.Height * scale;
                float drawX = bounds.Left + (bounds.Width  - drawW) / 2f - 20;
                float drawY = bounds.Top  + (bounds.Height - drawH) / 2f - 20;

                pe.Graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                pe.Graphics.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                pe.Graphics.DrawImage(bmp, drawX, drawY, drawW, drawH);

                pageIndex++;
                pe.HasMorePages = pageIndex < pages.Count;
            };

            try
            {
                pd.Print();
                MessageBox.Show("Лист персонажа экспортирован в PDF!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}", "Ошибка");
            }
        }

        // ─── Захват региона формы в Bitmap ────────────────────────────────────────
        /// <summary>
        /// Рендерит все контролы, пересекающиеся с заданным прямоугольником формы,
        /// в новый Bitmap. Контролы рисуются в порядке Z (от дальних к ближним).
        /// </summary>
        private Bitmap RenderPageRegion(int regionLeft, int regionTop, int regionWidth, int regionHeight)
        {
            var bmp = new Bitmap(regionWidth, regionHeight);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint  = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Собираем все контролы и сортируем от самого дальнего к ближнему
            // (ChildIndex меньше → ближе к пользователю; рисуем сначала дальние)
            var regionRect = new Rectangle(0, 0, regionWidth, regionHeight);

            var sorted = Enumerable.Range(0, this.Controls.Count)
                .Select(i => (ctrl: this.Controls[i], idx: i))
                .OrderByDescending(x => x.idx)   // высокий индекс = дальний
                .Select(x => x.ctrl)
                .ToList();

            foreach (Control ctrl in sorted)
            {
                // Пропускаем кнопку «назад»
                if (ctrl.Name == "pictureBox2") continue;

                int relX = ctrl.Left - regionLeft;
                int relY = ctrl.Top  - regionTop;
                var destRect = new Rectangle(relX, relY, ctrl.Width, ctrl.Height);

                if (!destRect.IntersectsWith(regionRect)) continue;

                try
                {
                    using var ctrlBmp = new Bitmap(ctrl.Width, ctrl.Height);
                    ctrl.DrawToBitmap(ctrlBmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height));
                    g.DrawImage(ctrlBmp, destRect);
                }
                catch
                {
                    // DrawToBitmap может не поддерживаться отдельными нативными контролами
                }
            }

            return bmp;
        }
    }
}
