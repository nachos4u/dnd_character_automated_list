using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DnD_character_list
{
    public partial class Form4
    {
        // ─── Таблица: имя навыка → чекбокс ────────────────────────────────────────
        private Dictionary<string, CheckBox> BuildSkillMap() =>
            new Dictionary<string, CheckBox>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["атлетика"]           = AthleticsCheckBox,
                ["акробатика"]         = AcrobaticsCheckBox,
                ["ловкость рук"]       = DexterityCheckBox,
                ["скрытность"]         = StealthCheckBox,
                ["анализ"]             = DessectionCheckBox,
                ["история"]            = HistoryCheckBox,
                ["магия"]              = MagicCheckBox,
                ["природа"]            = NatureCheckBox,
                ["религия"]            = ReligionCheckBox,
                ["восприятие"]         = PerceptionCheckBox,
                ["выживание"]          = SurvivalCheckBox,
                ["медицина"]           = MedicineCheckBox,
                ["проницательность"]   = DiscriminationCheckBox,
                ["проницание"]         = DiscriminationCheckBox,
                ["уход за животными"]  = AnimalCareCheckBox,
                ["выступление"]        = PerformanceCheckBox,
                ["запугивание"]        = IntimidationCheckBox,
                ["обман"]              = DeceptionCheckBox,
                ["убеждение"]          = PersuasionCheckBox,
            };

        // ─── Запуск анимации выбора навыков ───────────────────────────────────────
        private void StartSkillAnimation(List<string> skillNames, int requiredCount)
        {
            StopSkillAnimationOnly();
            var map = BuildSkillMap();

            // Резолвим все чекбоксы из списка доступных навыков класса
            var allCandidates = skillNames
                .Select(s => map.TryGetValue(s.Trim(), out var cb) ? cb : null)
                .Where(cb => cb != null)
                .ToList();

            // Сколько из них уже отмечено (например, от предыстории) — их не анимируем
            int alreadyChecked = allCandidates.Count(cb => cb.Checked);

            // Анимируем только незаполненные слоты
            _pendingCheckBoxes = allCandidates.Where(cb => !cb.Checked).ToList();
            _pendingSkillCount = System.Math.Max(0, requiredCount - alreadyChecked);

            // Если нужное количество уже выбрано — сразу завершаем
            if (_pendingSkillCount == 0)
            {
                StopSkillAnimation();
                return;
            }

            foreach (var cb in _pendingCheckBoxes)
                cb.CheckedChanged += CheckSkillProgress;

            _animTimer.Start();
        }

        // ─── Остановка анимации (только визуал, без записи в БД) ──────────────────
        private void StopSkillAnimationOnly()
        {
            _animTimer.Stop();
            foreach (var cb in _pendingCheckBoxes)
            {
                cb.BackColor = SystemColors.Control;
                cb.CheckedChanged -= CheckSkillProgress;
            }
            _pendingCheckBoxes.Clear();
        }

        // ─── Полная остановка анимации с сохранением в БД ─────────────────────────
        private void StopSkillAnimation()
        {
            StopSkillAnimationOnly();
            using (var db = new DDInformationContext())
            {
                var ch = db.Characters.Find(DataIDCharacter);
                if (ch != null)
                {
                    ch.SkillsPending = false;
                    db.SaveChanges();
                }
            }
        }

        // ─── Тик таймера: мигание незаполненных чекбоксов ────────────────────────
        private void AnimTimer_Tick(object sender, System.EventArgs e)
        {
            _animState = !_animState;
            foreach (var cb in _pendingCheckBoxes)
                cb.BackColor = (!cb.Checked && _animState) ? Color.LightYellow : SystemColors.Control;
        }

        // ─── Проверка прогресса: достаточно ли навыков выбрано ───────────────────
        private void CheckSkillProgress(object sender, System.EventArgs e)
        {
            if (!_animTimer.Enabled) return;
            if (_pendingCheckBoxes.Count(cb => cb.Checked) >= _pendingSkillCount)
                StopSkillAnimation();
        }
    }
}
