using WarChess.Core;

namespace WarChess.Terrain
{
    /// <summary>
    /// Pre-built terrain maps for campaign battles and multiplayer map rotation.
    /// Each method returns a TerrainMap with tiles set per the battle's terrain type.
    /// GDD Section 4.3: campaign terrain is hand-crafted per battle.
    /// </summary>
    public static class TerrainMapTemplates
    {
        /// <summary>Returns the terrain map for a campaign battle by number.</summary>
        public static TerrainMap GetCampaignMap(int battleNumber)
        {
            return battleNumber switch
            {
                1 => CreateOpenField(),
                2 => CreateCrossroads(),
                3 => CreateOpenField(),
                4 => CreateHillRidge(),
                5 => CreateHillAndOpen(),
                6 => CreateRiverCrossing(),
                7 => CreateForestHill(),
                8 => CreateFortifiedTown(),
                9 => CreateDenseForest(),
                10 => CreateNarrowPass(),
                11 => CreateOpenField(),
                12 => CreateMuddyPlains(),
                13 => CreateHeavyFortification(),
                14 => CreateForestRiver(),
                15 => CreateHillAndOpen(),
                16 => CreateDenseTown(),
                17 => CreateVaried(),
                18 => CreateRiverBridgeForest(),
                19 => CreateOpenRiver(),
                20 => CreateGrandBatteryHill(),
                21 => CreateWinterMarch(),
                22 => CreateHillAndOpen(),
                23 => CreateFortifiedDefense(),
                24 => CreateOpenField(),
                25 => CreateForestTown(),
                26 => CreateMuddyPlains(),
                27 => CreateHillFortification(),
                28 => CreateVaried(),
                29 => CreateComplexMap(),
                30 => CreateWaterloo(),
                _ => CreateOpenField()
            };
        }

        public static TerrainMap CreateOpenField()
        {
            return new TerrainMap(); // All open by default
        }

        public static TerrainMap CreateCrossroads()
        {
            var map = new TerrainMap();
            // Forest strips on left and right sides of the field
            for (int y = 5; y <= 8; y++)
            {
                map.SetTerrain(new GridCoord(1, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(2, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(9, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(10, y), TerrainType.Forest);
            }
            return map;
        }

        public static TerrainMap CreateHillRidge()
        {
            var map = new TerrainMap();
            // Ridge of hills across rows 4-5
            for (int x = 2; x <= 9; x++)
            {
                map.SetTerrain(new GridCoord(x, 4), TerrainType.Hill);
                map.SetTerrain(new GridCoord(x, 5), TerrainType.Hill);
            }
            return map;
        }

        public static TerrainMap CreateHillAndOpen()
        {
            var map = new TerrainMap();
            // Hills in center-north of field
            for (int x = 4; x <= 7; x++)
            {
                map.SetTerrain(new GridCoord(x, 6), TerrainType.Hill);
                map.SetTerrain(new GridCoord(x, 7), TerrainType.Hill);
            }
            return map;
        }

        public static TerrainMap CreateRiverCrossing()
        {
            var map = new TerrainMap();
            // River across row 5-6 with a bridge at column 5
            for (int x = 1; x <= 10; x++)
            {
                map.SetTerrain(new GridCoord(x, 5), TerrainType.River);
            }
            map.SetTerrain(new GridCoord(5, 5), TerrainType.Bridge);
            map.SetTerrain(new GridCoord(6, 5), TerrainType.Bridge);
            return map;
        }

        public static TerrainMap CreateForestHill()
        {
            var map = new TerrainMap();
            // Forest on left side, hills on right
            for (int y = 4; y <= 7; y++)
            {
                map.SetTerrain(new GridCoord(1, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(2, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(3, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(8, y), TerrainType.Hill);
                map.SetTerrain(new GridCoord(9, y), TerrainType.Hill);
            }
            return map;
        }

        public static TerrainMap CreateFortifiedTown()
        {
            var map = new TerrainMap();
            // Town center with fortifications around it
            for (int x = 4; x <= 7; x++)
                for (int y = 6; y <= 8; y++)
                    map.SetTerrain(new GridCoord(x, y), TerrainType.Town);

            map.SetTerrain(new GridCoord(3, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(8, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(5, 5), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(6, 5), TerrainType.Fortification);
            return map;
        }

        public static TerrainMap CreateDenseForest()
        {
            var map = new TerrainMap();
            // Heavy forest across the middle
            for (int x = 1; x <= 10; x++)
            {
                for (int y = 4; y <= 7; y++)
                {
                    if ((x + y) % 3 != 0) // Leave some gaps
                        map.SetTerrain(new GridCoord(x, y), TerrainType.Forest);
                }
            }
            return map;
        }

        public static TerrainMap CreateNarrowPass()
        {
            var map = new TerrainMap();
            // Rivers on both sides creating a narrow corridor
            for (int y = 3; y <= 8; y++)
            {
                map.SetTerrain(new GridCoord(1, y), TerrainType.River);
                map.SetTerrain(new GridCoord(2, y), TerrainType.River);
                map.SetTerrain(new GridCoord(9, y), TerrainType.River);
                map.SetTerrain(new GridCoord(10, y), TerrainType.River);
            }
            // Hills at the center of the pass
            map.SetTerrain(new GridCoord(5, 5), TerrainType.Hill);
            map.SetTerrain(new GridCoord(6, 5), TerrainType.Hill);
            return map;
        }

        public static TerrainMap CreateMuddyPlains()
        {
            var map = new TerrainMap();
            // Mud patches across the center
            for (int x = 2; x <= 9; x += 2)
            {
                map.SetTerrain(new GridCoord(x, 5), TerrainType.Mud);
                map.SetTerrain(new GridCoord(x, 6), TerrainType.Mud);
            }
            return map;
        }

        public static TerrainMap CreateHeavyFortification()
        {
            var map = new TerrainMap();
            // Fortified defensive position in rows 6-8
            for (int x = 3; x <= 8; x++)
            {
                map.SetTerrain(new GridCoord(x, 6), TerrainType.Fortification);
            }
            map.SetTerrain(new GridCoord(5, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(6, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(5, 8), TerrainType.Town);
            map.SetTerrain(new GridCoord(6, 8), TerrainType.Town);
            return map;
        }

        public static TerrainMap CreateForestRiver()
        {
            var map = new TerrainMap();
            // River with forest on both banks
            for (int x = 1; x <= 10; x++)
                map.SetTerrain(new GridCoord(x, 6), TerrainType.River);
            map.SetTerrain(new GridCoord(4, 6), TerrainType.Bridge);

            for (int x = 1; x <= 10; x += 2)
            {
                map.SetTerrain(new GridCoord(x, 5), TerrainType.Forest);
                map.SetTerrain(new GridCoord(x, 7), TerrainType.Forest);
            }
            return map;
        }

        public static TerrainMap CreateDenseTown()
        {
            var map = new TerrainMap();
            // Town tiles filling the center
            for (int x = 3; x <= 8; x++)
                for (int y = 4; y <= 7; y++)
                    map.SetTerrain(new GridCoord(x, y), TerrainType.Town);
            return map;
        }

        public static TerrainMap CreateVaried()
        {
            var map = new TerrainMap();
            // Mix of everything
            map.SetTerrain(new GridCoord(2, 5), TerrainType.Forest);
            map.SetTerrain(new GridCoord(3, 5), TerrainType.Forest);
            map.SetTerrain(new GridCoord(8, 5), TerrainType.Hill);
            map.SetTerrain(new GridCoord(9, 5), TerrainType.Hill);
            map.SetTerrain(new GridCoord(5, 6), TerrainType.Mud);
            map.SetTerrain(new GridCoord(6, 6), TerrainType.Mud);
            map.SetTerrain(new GridCoord(4, 7), TerrainType.Town);
            map.SetTerrain(new GridCoord(7, 7), TerrainType.Fortification);
            return map;
        }

        public static TerrainMap CreateRiverBridgeForest()
        {
            var map = new TerrainMap();
            for (int x = 1; x <= 10; x++)
                map.SetTerrain(new GridCoord(x, 5), TerrainType.River);
            map.SetTerrain(new GridCoord(3, 5), TerrainType.Bridge);
            map.SetTerrain(new GridCoord(7, 5), TerrainType.Bridge);
            for (int y = 6; y <= 8; y++)
            {
                map.SetTerrain(new GridCoord(1, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(2, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(9, y), TerrainType.Forest);
                map.SetTerrain(new GridCoord(10, y), TerrainType.Forest);
            }
            return map;
        }

        public static TerrainMap CreateOpenRiver()
        {
            var map = new TerrainMap();
            for (int x = 1; x <= 10; x++)
                map.SetTerrain(new GridCoord(x, 6), TerrainType.River);
            map.SetTerrain(new GridCoord(5, 6), TerrainType.Bridge);
            return map;
        }

        public static TerrainMap CreateGrandBatteryHill()
        {
            var map = new TerrainMap();
            // Long ridge for the artillery duel
            for (int x = 2; x <= 9; x++)
            {
                map.SetTerrain(new GridCoord(x, 3), TerrainType.Hill);
                map.SetTerrain(new GridCoord(x, 8), TerrainType.Hill);
            }
            return map;
        }

        public static TerrainMap CreateWinterMarch()
        {
            var map = new TerrainMap();
            // Mud everywhere with river crossings
            for (int x = 1; x <= 10; x++)
                for (int y = 4; y <= 7; y++)
                    map.SetTerrain(new GridCoord(x, y), TerrainType.Mud);

            for (int x = 1; x <= 10; x++)
                map.SetTerrain(new GridCoord(x, 5), TerrainType.River);
            map.SetTerrain(new GridCoord(3, 5), TerrainType.Bridge);
            map.SetTerrain(new GridCoord(8, 5), TerrainType.Bridge);
            return map;
        }

        public static TerrainMap CreateFortifiedDefense()
        {
            var map = new TerrainMap();
            // Player-side fortifications
            for (int x = 3; x <= 8; x++)
            {
                map.SetTerrain(new GridCoord(x, 3), TerrainType.Fortification);
                map.SetTerrain(new GridCoord(x, 4), TerrainType.Fortification);
            }
            return map;
        }

        public static TerrainMap CreateForestTown()
        {
            var map = new TerrainMap();
            // Alternating forest and town blocks
            for (int y = 4; y <= 7; y++)
            {
                for (int x = 1; x <= 10; x++)
                {
                    if ((x + y) % 2 == 0)
                        map.SetTerrain(new GridCoord(x, y), TerrainType.Forest);
                    else if (x >= 4 && x <= 7)
                        map.SetTerrain(new GridCoord(x, y), TerrainType.Town);
                }
            }
            return map;
        }

        public static TerrainMap CreateHillFortification()
        {
            var map = new TerrainMap();
            for (int x = 3; x <= 8; x++)
                map.SetTerrain(new GridCoord(x, 6), TerrainType.Hill);
            map.SetTerrain(new GridCoord(5, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(6, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(4, 8), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(7, 8), TerrainType.Fortification);
            return map;
        }

        public static TerrainMap CreateComplexMap()
        {
            var map = new TerrainMap();
            // Everything — used for Battle 29
            map.SetTerrain(new GridCoord(1, 5), TerrainType.River);
            map.SetTerrain(new GridCoord(2, 5), TerrainType.River);
            map.SetTerrain(new GridCoord(3, 5), TerrainType.Bridge);
            map.SetTerrain(new GridCoord(4, 5), TerrainType.Mud);
            map.SetTerrain(new GridCoord(5, 5), TerrainType.OpenField);
            map.SetTerrain(new GridCoord(6, 5), TerrainType.Hill);
            map.SetTerrain(new GridCoord(7, 5), TerrainType.Hill);
            map.SetTerrain(new GridCoord(8, 5), TerrainType.Forest);
            map.SetTerrain(new GridCoord(9, 5), TerrainType.Forest);
            map.SetTerrain(new GridCoord(10, 5), TerrainType.Town);

            map.SetTerrain(new GridCoord(3, 7), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(4, 7), TerrainType.Town);
            map.SetTerrain(new GridCoord(7, 7), TerrainType.Forest);
            map.SetTerrain(new GridCoord(8, 7), TerrainType.Hill);

            map.SetTerrain(new GridCoord(5, 6), TerrainType.Mud);
            map.SetTerrain(new GridCoord(6, 6), TerrainType.Mud);
            return map;
        }

        public static TerrainMap CreateWaterloo()
        {
            var map = new TerrainMap();
            // Iconic layout: ridge for the Allied position, open approach
            // Player ridge (rows 3-4)
            for (int x = 3; x <= 8; x++)
                map.SetTerrain(new GridCoord(x, 4), TerrainType.Hill);

            // Hougoumont (fortified farm, left flank)
            map.SetTerrain(new GridCoord(2, 5), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(2, 6), TerrainType.Fortification);
            map.SetTerrain(new GridCoord(3, 5), TerrainType.Town);

            // La Haye Sainte (center farm)
            map.SetTerrain(new GridCoord(5, 6), TerrainType.Town);
            map.SetTerrain(new GridCoord(6, 6), TerrainType.Town);

            // Mud from rain
            map.SetTerrain(new GridCoord(4, 5), TerrainType.Mud);
            map.SetTerrain(new GridCoord(7, 5), TerrainType.Mud);

            // Enemy ridge (rows 8-9)
            for (int x = 3; x <= 8; x++)
                map.SetTerrain(new GridCoord(x, 8), TerrainType.Hill);

            return map;
        }
    }
}
