using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DnD_character_list.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "class_table_features",
                table: "Level",
                type: "text",
                nullable: true);
        }

        void _OriginalUp_Unused(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Background",
                columns: table => new
                {
                    ID_background = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    possesion = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    gm = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    invetary = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    toolOwnership = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Background", x => x.ID_background);
                });

            migrationBuilder.CreateTable(
                name: "Hit_dice",
                columns: table => new
                {
                    ID_hit_dice = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hit_dice = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hit_dice", x => x.ID_hit_dice);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ID_item = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.ID_item);
                });

            migrationBuilder.CreateTable(
                name: "Species",
                columns: table => new
                {
                    ID_species = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    species_skills = table.Column<string>(type: "text", nullable: false),
                    species_chatics = table.Column<string>(name: "species_cha-tics", type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    speed = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => x.ID_species);
                });

            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    ID_spell = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    peculiarities = table.Column<string>(type: "text", nullable: false),
                    cell_level = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    school = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    components = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    range = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    duration = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    time = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    material_component = table.Column<string>(type: "text", nullable: true),
                    upper = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells_1", x => x.ID_spell);
                });

            migrationBuilder.CreateTable(
                name: "Traits",
                columns: table => new
                {
                    ID_trait = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chartics = table.Column<string>(name: "char-tics", type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traits", x => x.ID_trait);
                });

            migrationBuilder.CreateTable(
                name: "Class",
                columns: table => new
                {
                    ID_class = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    possession = table.Column<string>(type: "text", nullable: false),
                    ID_hit_dice = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Class", x => x.ID_class);
                    table.ForeignKey(
                        name: "FK_Class_Hit_dice",
                        column: x => x.ID_hit_dice,
                        principalTable: "Hit_dice",
                        principalColumn: "ID_hit_dice");
                });

            migrationBuilder.CreateTable(
                name: "Character",
                columns: table => new
                {
                    ID_character = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_species = table.Column<int>(type: "integer", nullable: false),
                    ID_background = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    possession = table.Column<string>(type: "text", nullable: true),
                    kd = table.Column<int>(type: "integer", nullable: true),
                    worldview = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    hitpoints = table.Column<int>(type: "integer", nullable: true),
                    time_hitpoints = table.Column<int>(type: "integer", nullable: true),
                    characteristiks = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: true),
                    speed = table.Column<int>(type: "integer", nullable: true),
                    exp = table.Column<int>(type: "integer", nullable: true),
                    spas_win = table.Column<int>(type: "integer", nullable: true),
                    cur_hp = table.Column<int>(type: "integer", nullable: true),
                    spas_lose = table.Column<int>(type: "integer", nullable: true),
                    possesion_new = table.Column<string>(type: "text", nullable: true),
                    gm = table.Column<int>(type: "integer", nullable: true),
                    sm = table.Column<int>(type: "integer", nullable: true),
                    mm = table.Column<int>(type: "integer", nullable: true),
                    em = table.Column<int>(type: "integer", nullable: true),
                    pm = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Character", x => x.ID_character);
                    table.ForeignKey(
                        name: "FK_Character_Background",
                        column: x => x.ID_background,
                        principalTable: "Background",
                        principalColumn: "ID_background");
                    table.ForeignKey(
                        name: "FK_Character_Species",
                        column: x => x.ID_species,
                        principalTable: "Species",
                        principalColumn: "ID_species");
                });

            migrationBuilder.CreateTable(
                name: "Level",
                columns: table => new
                {
                    level = table.Column<int>(type: "integer", nullable: false),
                    ID_class = table.Column<int>(type: "integer", nullable: false),
                    cells = table.Column<string>(type: "text", nullable: true),
                    skills = table.Column<string>(type: "text", nullable: true),
                    class_table_features = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Level", x => new { x.level, x.ID_class });
                    table.ForeignKey(
                        name: "FK_Level_Class",
                        column: x => x.ID_class,
                        principalTable: "Class",
                        principalColumn: "ID_class");
                });

            migrationBuilder.CreateTable(
                name: "Item_inventory",
                columns: table => new
                {
                    ID_item = table.Column<int>(type: "integer", nullable: false),
                    ID_character = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item_inventory", x => new { x.ID_item, x.ID_character });
                    table.ForeignKey(
                        name: "FK_Item_inventory_Character",
                        column: x => x.ID_character,
                        principalTable: "Character",
                        principalColumn: "ID_character");
                    table.ForeignKey(
                        name: "FK_Item_inventory_Items",
                        column: x => x.ID_item,
                        principalTable: "Items",
                        principalColumn: "ID_item");
                });

            migrationBuilder.CreateTable(
                name: "Spell_inventory",
                columns: table => new
                {
                    ID_spell = table.Column<int>(type: "integer", nullable: false),
                    ID_character = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spell_inventory", x => new { x.ID_spell, x.ID_character });
                    table.ForeignKey(
                        name: "FK_Spell_inventory_Character",
                        column: x => x.ID_character,
                        principalTable: "Character",
                        principalColumn: "ID_character");
                    table.ForeignKey(
                        name: "FK_Spell_inventory_Spells",
                        column: x => x.ID_spell,
                        principalTable: "Spells",
                        principalColumn: "ID_spell");
                });

            migrationBuilder.CreateTable(
                name: "Trait_inventory",
                columns: table => new
                {
                    ID_trait = table.Column<int>(type: "integer", nullable: false),
                    ID_character = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trait_inventory", x => new { x.ID_trait, x.ID_character });
                    table.ForeignKey(
                        name: "FK_Trait_inventory_Character",
                        column: x => x.ID_character,
                        principalTable: "Character",
                        principalColumn: "ID_character");
                    table.ForeignKey(
                        name: "FK_Trait_inventory_Traits",
                        column: x => x.ID_trait,
                        principalTable: "Traits",
                        principalColumn: "ID_trait");
                });

            migrationBuilder.CreateTable(
                name: "Multiclass",
                columns: table => new
                {
                    level = table.Column<int>(type: "integer", nullable: false),
                    ID_character = table.Column<int>(type: "integer", nullable: false),
                    ID_class = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Multiclass", x => new { x.level, x.ID_character, x.ID_class });
                    table.ForeignKey(
                        name: "FK_Multiclass_Character",
                        column: x => x.ID_character,
                        principalTable: "Character",
                        principalColumn: "ID_character");
                    table.ForeignKey(
                        name: "FK_Multiclass_Level",
                        columns: x => new { x.level, x.ID_class },
                        principalTable: "Level",
                        principalColumns: new[] { "level", "ID_class" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Character_ID_background",
                table: "Character",
                column: "ID_background");

            migrationBuilder.CreateIndex(
                name: "IX_Character_ID_species",
                table: "Character",
                column: "ID_species");

            migrationBuilder.CreateIndex(
                name: "IX_Class_ID_hit_dice",
                table: "Class",
                column: "ID_hit_dice");

            migrationBuilder.CreateIndex(
                name: "IX_Item_inventory_ID_character",
                table: "Item_inventory",
                column: "ID_character");

            migrationBuilder.CreateIndex(
                name: "IX_Level_ID_class",
                table: "Level",
                column: "ID_class");

            migrationBuilder.CreateIndex(
                name: "IX_Multiclass_ID_character",
                table: "Multiclass",
                column: "ID_character");

            migrationBuilder.CreateIndex(
                name: "IX_Multiclass_level_ID_class",
                table: "Multiclass",
                columns: new[] { "level", "ID_class" });

            migrationBuilder.CreateIndex(
                name: "IX_Spell_inventory_ID_character",
                table: "Spell_inventory",
                column: "ID_character");

            migrationBuilder.CreateIndex(
                name: "IX_Trait_inventory_ID_character",
                table: "Trait_inventory",
                column: "ID_character");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "class_table_features",
                table: "Level");
        }

        void _OriginalDown_Unused(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Item_inventory");

            migrationBuilder.DropTable(
                name: "Multiclass");

            migrationBuilder.DropTable(
                name: "Spell_inventory");

            migrationBuilder.DropTable(
                name: "Trait_inventory");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Level");

            migrationBuilder.DropTable(
                name: "Spells");

            migrationBuilder.DropTable(
                name: "Character");

            migrationBuilder.DropTable(
                name: "Traits");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Background");

            migrationBuilder.DropTable(
                name: "Species");

            migrationBuilder.DropTable(
                name: "Hit_dice");
        }
    }
}
