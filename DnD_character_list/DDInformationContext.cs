using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DnD_character_list;

public partial class DDInformationContext : DbContext
{
    public DDInformationContext()
    {
    }

    public DDInformationContext(DbContextOptions<DDInformationContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Background> Backgrounds { get; set; }

    public virtual DbSet<Character> Characters { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<HitDice> HitDices { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemInventory> ItemInventories { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<Species> Species { get; set; }

    public virtual DbSet<Spell> Spells { get; set; }

    public virtual DbSet<Trait> Traits { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Database=dnd_information;Username=postgres;Password=eto_CATAHA2006");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Background>(entity =>
        {
            entity.HasKey(e => e.IdBackground);

            entity.ToTable("Background");

            entity.Property(e => e.IdBackground).HasColumnName("ID_background");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Gm).HasColumnName("gm");
            entity.Property(e => e.Invetary).HasColumnName("invetary");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Possesion).HasColumnName("possesion");
            entity.Property(e => e.Source).HasMaxLength(5).HasColumnName("source");
            entity.Property(e => e.ToolOwnership).HasColumnName("toolOwnership");
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.IdCharacter);

            entity.ToTable("Character");

            entity.Property(e => e.IdCharacter).HasColumnName("ID_character");
            entity.Property(e => e.Characteristiks).HasMaxLength(42).HasColumnName("characteristiks");
            entity.Property(e => e.CurHp).HasColumnName("cur_hp");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Em).HasColumnName("em");
            entity.Property(e => e.Exp).HasColumnName("exp");
            entity.Property(e => e.Gm).HasColumnName("gm");
            entity.Property(e => e.Hitpoints).HasColumnName("hitpoints");
            entity.Property(e => e.IdBackground).HasColumnName("ID_background");
            entity.Property(e => e.IdSpecies).HasColumnName("ID_species");
            entity.Property(e => e.Kd).HasColumnName("kd");
            entity.Property(e => e.Mm).HasColumnName("mm");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Pm).HasColumnName("pm");
            entity.Property(e => e.PossesionNew).HasColumnName("possesion_new");
            entity.Property(e => e.Possession).HasColumnName("possession");
            entity.Property(e => e.Sm).HasColumnName("sm");
            entity.Property(e => e.SpasLose).HasColumnName("spas_lose");
            entity.Property(e => e.SpasWin).HasColumnName("spas_win");
            entity.Property(e => e.Speed).HasColumnName("speed");
            entity.Property(e => e.TimeHitpoints).HasColumnName("time_hitpoints");
            entity.Property(e => e.Worldview).HasMaxLength(2).HasColumnName("worldview");
            entity.Property(e => e.PrimaryClassId).HasColumnName("primary_class_id");
            entity.Property(e => e.SkillsPending).HasColumnName("skills_pending").HasDefaultValue(false);
            entity.Property(e => e.PendingSkillChoices).HasColumnName("pending_skill_choices");
            entity.Property(e => e.PendingSkillCount).HasColumnName("pending_skill_count");

            entity.HasOne(d => d.IdBackgroundNavigation).WithMany(p => p.Characters)
                .HasForeignKey(d => d.IdBackground)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Character_Background");

            entity.HasOne(d => d.IdSpeciesNavigation).WithMany(p => p.Characters)
                .HasForeignKey(d => d.IdSpecies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Character_Species");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.IdClass);

            entity.ToTable("Class");

            entity.Property(e => e.IdClass).HasColumnName("ID_class");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IdHitDice).HasColumnName("ID_hit_dice");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
            entity.Property(e => e.Possession).HasColumnName("possession");

            entity.HasOne(d => d.IdHitDiceNavigation).WithMany(p => p.Classes)
                .HasForeignKey(d => d.IdHitDice)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Hit_dice");
        });

        modelBuilder.Entity<HitDice>(entity =>
        {
            entity.HasKey(e => e.IdHitDice);

            entity.ToTable("Hit_dice");

            entity.Property(e => e.IdHitDice).HasColumnName("ID_hit_dice");
            entity.Property(e => e.HitDice1).HasMaxLength(3).HasColumnName("hit_dice");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.IdItem);

            entity.Property(e => e.IdItem).HasColumnName("ID_item");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Source).HasMaxLength(10).HasColumnName("source");
            entity.Property(e => e.Price).HasMaxLength(50).HasColumnName("price");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.IsMagic).HasColumnName("is_magic").HasDefaultValue(false);
            entity.Property(e => e.Rarity).HasMaxLength(50).HasColumnName("rarity");
            entity.Property(e => e.ItemType).HasMaxLength(100).HasColumnName("item_type");
        });

        modelBuilder.Entity<ItemInventory>(entity =>
        {
            entity.HasKey(e => new { e.IdItem, e.IdCharacter });

            entity.ToTable("Item_inventory");

            entity.Property(e => e.IdItem).HasColumnName("ID_item");
            entity.Property(e => e.IdCharacter).HasColumnName("ID_character");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.IdCharacterNavigation).WithMany(p => p.ItemInventories)
                .HasForeignKey(d => d.IdCharacter)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Item_inventory_Character");

            entity.HasOne(d => d.IdItemNavigation).WithMany(p => p.ItemInventories)
                .HasForeignKey(d => d.IdItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Item_inventory_Items");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => new { e.Level1, e.IdClass });

            entity.ToTable("Level");

            entity.Property(e => e.Level1).HasColumnName("level");
            entity.Property(e => e.IdClass).HasColumnName("ID_class");
            entity.Property(e => e.Cells).HasColumnName("cells");
            entity.Property(e => e.Skills).HasColumnName("skills");
            entity.Property(e => e.ClassTableFeatures).HasColumnName("class_table_features");

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.Levels)
                .HasForeignKey(d => d.IdClass)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Level_Class");

            entity.HasMany(d => d.IdCharacters).WithMany(p => p.Levels)
                .UsingEntity<Dictionary<string, object>>(
                    "Multiclass",
                    r => r.HasOne<Character>().WithMany()
                        .HasForeignKey("IdCharacter")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_Multiclass_Character"),
                    l => l.HasOne<Level>().WithMany()
                        .HasForeignKey("Level", "IdClass")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_Multiclass_Level"),
                    j =>
                    {
                        j.HasKey("Level", "IdCharacter", "IdClass");
                        j.ToTable("Multiclass");
                        j.IndexerProperty<int>("Level").HasColumnName("level");
                        j.IndexerProperty<int>("IdCharacter").HasColumnName("ID_character");
                        j.IndexerProperty<int>("IdClass").HasColumnName("ID_class");
                    });
        });

        modelBuilder.Entity<Species>(entity =>
        {
            entity.HasKey(e => e.IdSpecies);

            entity.Property(e => e.IdSpecies).HasColumnName("ID_species");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
            entity.Property(e => e.Source).HasMaxLength(5).HasColumnName("source");
            entity.Property(e => e.SpeciesChaTics).HasMaxLength(50).HasColumnName("species_cha-tics");
            entity.Property(e => e.SpeciesSkills).HasColumnName("species_skills");
            entity.Property(e => e.Speed).HasColumnName("speed");
        });

        modelBuilder.Entity<Spell>(entity =>
        {
            entity.HasKey(e => e.IdSpell).HasName("PK_Spells_1");

            entity.Property(e => e.IdSpell).HasColumnName("ID_spell");
            entity.Property(e => e.CellLevel).HasColumnName("cell_level");
            entity.Property(e => e.Components).HasMaxLength(15).HasColumnName("components");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duration).HasMaxLength(70).HasColumnName("duration");
            entity.Property(e => e.MaterialComponent).HasColumnName("material_component");
            entity.Property(e => e.Name).HasMaxLength(70).HasColumnName("name");
            entity.Property(e => e.Peculiarities).HasColumnName("peculiarities");
            entity.Property(e => e.Range).HasMaxLength(70).HasColumnName("range");
            entity.Property(e => e.School).HasMaxLength(40).HasColumnName("school");
            entity.Property(e => e.Source).HasMaxLength(5).HasColumnName("source");
            entity.Property(e => e.Time).HasMaxLength(70).HasColumnName("time");
            entity.Property(e => e.Upper).HasColumnName("upper");

            entity.HasMany(d => d.IdCharacters).WithMany(p => p.IdSpells)
                .UsingEntity<Dictionary<string, object>>(
                    "SpellInventory",
                    r => r.HasOne<Character>().WithMany()
                        .HasForeignKey("IdCharacter")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_Spell_inventory_Character"),
                    l => l.HasOne<Spell>().WithMany()
                        .HasForeignKey("IdSpell")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_Spell_inventory_Spells"),
                    j =>
                    {
                        j.HasKey("IdSpell", "IdCharacter");
                        j.ToTable("Spell_inventory");
                        j.IndexerProperty<int>("IdSpell").HasColumnName("ID_spell");
                        j.IndexerProperty<int>("IdCharacter").HasColumnName("ID_character");
                    });
        });

        modelBuilder.Entity<Trait>(entity =>
        {
            entity.HasKey(e => e.IdTrait);

            entity.Property(e => e.IdTrait).HasColumnName("ID_trait");
            entity.Property(e => e.CharTics).HasMaxLength(50).HasColumnName("char-tics").IsRequired(false);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Requirements).HasColumnName("requirements");
            entity.Property(e => e.Source).HasMaxLength(10).HasColumnName("source");

            entity.HasMany(d => d.IdCharacters).WithMany(p => p.IdTraits)
                .UsingEntity<Dictionary<string, object>>(
                    "TraitInventory",
                    r => r.HasOne<Character>().WithMany()
                        .HasForeignKey("IdCharacter")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_Trait_inventory_Character"),
                    l => l.HasOne<Trait>().WithMany()
                        .HasForeignKey("IdTrait")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_Trait_inventory_Traits"),
                    j =>
                    {
                        j.HasKey("IdTrait", "IdCharacter");
                        j.ToTable("Trait_inventory");
                        j.IndexerProperty<int>("IdTrait").HasColumnName("ID_trait");
                        j.IndexerProperty<int>("IdCharacter").HasColumnName("ID_character");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
