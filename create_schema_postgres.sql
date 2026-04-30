-- PostgreSQL schema for DnD_character_list
-- Run this script after creating the database:
--   CREATE DATABASE dnd_information;
--   \c dnd_information

CREATE TABLE "Hit_dice" (
    "ID_hit_dice" SERIAL PRIMARY KEY,
    "hit_dice"    VARCHAR(3)
);

CREATE TABLE "Background" (
    "ID_background" SERIAL PRIMARY KEY,
    "name"          VARCHAR(50),
    "description"   TEXT,
    "possesion"     TEXT,
    "invetary"      TEXT,
    "toolOwnership" TEXT,
    "gm"            INTEGER,
    "source"        VARCHAR(5)
);

CREATE TABLE "Species" (
    "ID_species"       SERIAL PRIMARY KEY,
    "name"             VARCHAR(50),
    "description"      TEXT,
    "species_skills"   TEXT,
    "species_cha-tics" VARCHAR(50),
    "speed"            TEXT,
    "source"           VARCHAR(5)
);

CREATE TABLE "Class" (
    "ID_class"    SERIAL PRIMARY KEY,
    "ID_hit_dice" INTEGER NOT NULL,
    "name"        VARCHAR(50),
    "description" TEXT,
    "possession"  TEXT,
    CONSTRAINT "FK_Class_Hit_dice" FOREIGN KEY ("ID_hit_dice") REFERENCES "Hit_dice" ("ID_hit_dice")
);

CREATE TABLE "Character" (
    "ID_character"    SERIAL PRIMARY KEY,
    "ID_species"      INTEGER NOT NULL,
    "ID_background"   INTEGER NOT NULL,
    "name"            VARCHAR(50),
    "description"     TEXT,
    "possession"      TEXT,
    "possesion_new"   TEXT,
    "characteristiks" VARCHAR(42),
    "hitpoints"       INTEGER,
    "cur_hp"          INTEGER,
    "time_hitpoints"  INTEGER,
    "speed"           INTEGER,
    "kd"              INTEGER,
    "worldview"       VARCHAR(2),
    "exp"             INTEGER,
    "spas_win"        INTEGER,
    "spas_lose"       INTEGER,
    "gm"              INTEGER,
    "sm"              INTEGER,
    "mm"              INTEGER,
    "em"              INTEGER,
    "pm"              INTEGER,
    "notes"           TEXT,
    CONSTRAINT "FK_Character_Background" FOREIGN KEY ("ID_background") REFERENCES "Background" ("ID_background"),
    CONSTRAINT "FK_Character_Species"    FOREIGN KEY ("ID_species")    REFERENCES "Species"    ("ID_species")
);

CREATE TABLE "Items" (
    "ID_item"     SERIAL PRIMARY KEY,
    "name"        VARCHAR(50),
    "description" TEXT
);

CREATE TABLE "Item_inventory" (
    "ID_item"      INTEGER NOT NULL,
    "ID_character" INTEGER NOT NULL,
    "quantity"     INTEGER,
    PRIMARY KEY ("ID_item", "ID_character"),
    CONSTRAINT "FK_Item_inventory_Character" FOREIGN KEY ("ID_character") REFERENCES "Character" ("ID_character"),
    CONSTRAINT "FK_Item_inventory_Items"     FOREIGN KEY ("ID_item")      REFERENCES "Items"     ("ID_item")
);

CREATE TABLE "Level" (
    "level"    INTEGER NOT NULL,
    "ID_class" INTEGER NOT NULL,
    "cells"    TEXT,
    "skills"   TEXT,
    PRIMARY KEY ("level", "ID_class"),
    CONSTRAINT "FK_Level_Class" FOREIGN KEY ("ID_class") REFERENCES "Class" ("ID_class")
);

CREATE TABLE "Spells" (
    "ID_spell"           SERIAL,
    "name"               VARCHAR(70),
    "description"        TEXT,
    "peculiarities"      TEXT,
    "cell_level"         INTEGER,
    "source"             VARCHAR(5),
    "school"             VARCHAR(40),
    "components"         VARCHAR(15),
    "range"              VARCHAR(70),
    "duration"           VARCHAR(70),
    "time"               VARCHAR(70),
    "material_component" TEXT,
    "upper"              TEXT,
    CONSTRAINT "PK_Spells_1" PRIMARY KEY ("ID_spell")
);

CREATE TABLE "Traits" (
    "ID_trait"    SERIAL PRIMARY KEY,
    "char-tics"   VARCHAR(50),
    "description" TEXT
);

CREATE TABLE "Multiclass" (
    "level"        INTEGER NOT NULL,
    "ID_character" INTEGER NOT NULL,
    "ID_class"     INTEGER NOT NULL,
    PRIMARY KEY ("level", "ID_character", "ID_class"),
    CONSTRAINT "FK_Multiclass_Character" FOREIGN KEY ("ID_character")          REFERENCES "Character" ("ID_character"),
    CONSTRAINT "FK_Multiclass_Level"     FOREIGN KEY ("level", "ID_class") REFERENCES "Level"     ("level", "ID_class")
);

CREATE TABLE "Spell_inventory" (
    "ID_spell"     INTEGER NOT NULL,
    "ID_character" INTEGER NOT NULL,
    PRIMARY KEY ("ID_spell", "ID_character"),
    CONSTRAINT "FK_Spell_inventory_Character" FOREIGN KEY ("ID_character") REFERENCES "Character" ("ID_character"),
    CONSTRAINT "FK_Spell_inventory_Spells"    FOREIGN KEY ("ID_spell")     REFERENCES "Spells"    ("ID_spell")
);

CREATE TABLE "Trait_inventory" (
    "ID_trait"     INTEGER NOT NULL,
    "ID_character" INTEGER NOT NULL,
    PRIMARY KEY ("ID_trait", "ID_character"),
    CONSTRAINT "FK_Trait_inventory_Character" FOREIGN KEY ("ID_character") REFERENCES "Character" ("ID_character"),
    CONSTRAINT "FK_Trait_inventory_Traits"    FOREIGN KEY ("ID_trait")     REFERENCES "Traits"    ("ID_trait")
);
