using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnD_character_list
{
    // ─── DTO ──────────────────────────────────────────────────────────────────────
    public class CharacterDto
    {
        public string Name { get; set; }
        public int? Hitpoints { get; set; }
        public int? CurHp { get; set; }
        public int? Exp { get; set; }
        public string Notes { get; set; }
        public int? Speed { get; set; }
        public int? Gm { get; set; }
        public int? Mm { get; set; }
        public int? Sm { get; set; }
        public int? Em { get; set; }
        public int? Pm { get; set; }
        public string Characteristiks { get; set; }
        public string Description { get; set; }
        public string Possession { get; set; }
        public int? Kd { get; set; }
        public int? SpasWin { get; set; }
        public int? SpasLose { get; set; }
        public string PossesionNew { get; set; }
        public int? TimeHitpoints { get; set; }
        public string SpeciesName { get; set; }
        public string BackgroundName { get; set; }
        public List<CharacterLevelDto> Levels { get; set; } = new();
    }

    public class CharacterLevelDto
    {
        public string ClassName { get; set; }
        public int Level { get; set; }
    }

    // ─── Сериализатор ─────────────────────────────────────────────────────────────
    public class CharacterJsonSerializer
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // ── Экспорт ──────────────────────────────────────────────────────────────
        public string ExportToJson(int characterId)
        {
            using var db = new DDInformationContext();

            var character = db.Characters
                .Include(c => c.Levels).ThenInclude(l => l.IdClassNavigation)
                .Include(c => c.IdBackgroundNavigation)
                .Include(c => c.IdSpeciesNavigation)
                .FirstOrDefault(c => c.IdCharacter == characterId);

            if (character == null) return null;

            var dto = new CharacterDto
            {
                Name            = character.Name,
                Hitpoints       = character.Hitpoints,
                CurHp           = character.CurHp,
                Exp             = character.Exp,
                Notes           = character.Notes,
                Speed           = character.Speed,
                Gm              = character.Gm,
                Mm              = character.Mm,
                Sm              = character.Sm,
                Em              = character.Em,
                Pm              = character.Pm,
                Characteristiks = character.Characteristiks,
                Description     = character.Description,
                Possession      = character.Possession,
                Kd              = character.Kd,
                SpasWin         = character.SpasWin,
                SpasLose        = character.SpasLose,
                PossesionNew    = character.PossesionNew,
                TimeHitpoints   = character.TimeHitpoints,
                SpeciesName     = character.IdSpeciesNavigation?.Name,
                BackgroundName  = character.IdBackgroundNavigation?.Name,
                Levels          = character.Levels
                    .GroupBy(l => l.IdClass)
                    .Select(g => new CharacterLevelDto
                    {
                        ClassName = g.First().IdClassNavigation?.Name ?? "",
                        Level     = g.Max(l => l.Level1)
                    })
                    .ToList()
            };

            return JsonSerializer.Serialize(dto, _options);
        }

        // ── Импорт ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Создаёт нового персонажа из JSON. Возвращает true при успехе.
        /// </summary>
        public bool ImportFromJson(string json, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var dto = JsonSerializer.Deserialize<CharacterDto>(json, _options);
                if (dto == null)
                {
                    errorMessage = "Неверный формат JSON";
                    return false;
                }

                using var db = new DDInformationContext();

                // Ищем вид по имени; если не найден — берём первый доступный
                int speciesId = (string.IsNullOrEmpty(dto.SpeciesName)
                        ? null
                        : db.Species.FirstOrDefault(s => s.Name == dto.SpeciesName))
                    ?.IdSpecies
                    ?? db.Species.Select(s => s.IdSpecies).FirstOrDefault();

                // Ищем предысторию по имени; если не найдена — берём первую доступную
                int backgroundId = (string.IsNullOrEmpty(dto.BackgroundName)
                        ? null
                        : db.Backgrounds.FirstOrDefault(b => b.Name == dto.BackgroundName))
                    ?.IdBackground
                    ?? db.Backgrounds.Select(b => b.IdBackground).FirstOrDefault();

                if (speciesId == 0 || backgroundId == 0)
                {
                    errorMessage = "В базе данных нет ни одного вида или предыстории. Импортируйте справочники перед персонажем.";
                    return false;
                }

                var character = new Character
                {
                    Name            = dto.Name,
                    Hitpoints       = dto.Hitpoints,
                    CurHp           = dto.CurHp,
                    Exp             = dto.Exp,
                    Notes           = dto.Notes,
                    Speed           = dto.Speed,
                    Gm              = dto.Gm,
                    Mm              = dto.Mm,
                    Sm              = dto.Sm,
                    Em              = dto.Em,
                    Pm              = dto.Pm,
                    Characteristiks = dto.Characteristiks,
                    Description     = dto.Description,
                    Possession      = dto.Possession,
                    Kd              = dto.Kd,
                    SpasWin         = dto.SpasWin,
                    SpasLose        = dto.SpasLose,
                    PossesionNew    = dto.PossesionNew,
                    TimeHitpoints   = dto.TimeHitpoints,
                    IdSpecies       = speciesId,
                    IdBackground    = backgroundId
                };

                db.Characters.Add(character);
                db.SaveChanges(); // получаем IdCharacter

                // Добавляем уровни классов
                if (dto.Levels?.Count > 0)
                {
                    var fullChar = db.Characters
                        .Include(c => c.Levels)
                        .First(c => c.IdCharacter == character.IdCharacter);

                    foreach (var lvlDto in dto.Levels)
                    {
                        if (string.IsNullOrEmpty(lvlDto.ClassName)) continue;

                        // Ищем класс: точное совпадение, потом частичное по базовому имени
                        string baseName = lvlDto.ClassName.Split(':')[0].Trim();
                        var cls = db.Classes.FirstOrDefault(c => c.Name == lvlDto.ClassName)
                               ?? db.Classes.FirstOrDefault(c => c.Name.StartsWith(baseName + ":")
                                                              || c.Name == baseName);
                        if (cls == null) continue;

                        var levelsToAdd = db.Levels
                            .Where(l => l.IdClass == cls.IdClass && l.Level1 <= lvlDto.Level)
                            .ToList();

                        foreach (var lev in levelsToAdd)
                            fullChar.Levels.Add(lev);
                    }
                    db.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.InnerException?.Message ?? ex.Message;
                return false;
            }
        }
    }
}
