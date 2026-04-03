using System.Collections.Generic;
using WarChess.Units;

namespace WarChess.Config
{
    /// <summary>
    /// Colorblind palette options.
    /// </summary>
    public enum ColorblindPaletteType
    {
        Normal,
        Deuteranopia,
        Tritanopia
    }

    /// <summary>
    /// A color represented as RGBA bytes. No UnityEngine.Color dependency.
    /// </summary>
    public struct ColorRgba
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public ColorRgba(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>Returns hex string (e.g., "#FF0000FF").</summary>
        public override string ToString()
        {
            return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
        }
    }

    /// <summary>
    /// Provides accessibility data: colorblind palettes, text scaling,
    /// and unit label generation. Pure C# — UI layer applies the values.
    /// </summary>
    public class AccessibilityManager
    {
        private readonly int _colorblindPaletteIndex;
        private readonly int _textSize;
        private readonly bool _colorblindMode;

        /// <summary>
        /// Creates accessibility manager from player settings values.
        /// </summary>
        /// <param name="colorblindMode">Whether colorblind mode is enabled.</param>
        /// <param name="colorblindPaletteIndex">0=Normal, 1=Deuteranopia, 2=Tritanopia.</param>
        /// <param name="textSize">0=Small, 1=Medium, 2=Large.</param>
        public AccessibilityManager(bool colorblindMode, int colorblindPaletteIndex, int textSize)
        {
            _colorblindMode = colorblindMode;
            _colorblindPaletteIndex = colorblindPaletteIndex;
            _textSize = textSize;
        }

        // --- Colorblind Palettes ---

        /// <summary>
        /// Returns the color palette for the current colorblind setting.
        /// </summary>
        public Dictionary<string, ColorRgba> GetCurrentPalette()
        {
            var type = (ColorblindPaletteType)_colorblindPaletteIndex;
            return GetPalette(type);
        }

        /// <summary>
        /// Returns a specific palette by type.
        /// Semantic keys: "PlayerUnit", "EnemyUnit", "Infantry", "Cavalry", "Artillery",
        /// "Terrain.Forest", "Terrain.Hill", "Terrain.River", etc.
        /// </summary>
        public static Dictionary<string, ColorRgba> GetPalette(ColorblindPaletteType type)
        {
            switch (type)
            {
                case ColorblindPaletteType.Deuteranopia:
                    return BuildDeuteranopiaPalette();
                case ColorblindPaletteType.Tritanopia:
                    return BuildTritanopiaPalette();
                default:
                    return BuildNormalPalette();
            }
        }

        // --- Text Scaling ---

        /// <summary>
        /// Returns the font size multiplier as a percentage for the current TextSize setting.
        /// 0 = 80%, 1 = 100%, 2 = 130%.
        /// </summary>
        public int GetTextScalePercent()
        {
            switch (_textSize)
            {
                case 0: return 80;
                case 2: return 130;
                default: return 100;
            }
        }

        /// <summary>
        /// Scales a base font size by the current text scale setting.
        /// Returns integer result: (baseFontSize * scalePercent) / 100.
        /// </summary>
        public int ScaleFontSize(int baseFontSize)
        {
            return (baseFontSize * GetTextScalePercent()) / 100;
        }

        // --- Unit Labels ---

        /// <summary>
        /// Generates a short text label for a unit type (for colorblind mode).
        /// Labels are 2-3 characters for readability on the grid.
        /// </summary>
        public static string GetUnitLabel(UnitType type)
        {
            switch (type)
            {
                case UnitType.LineInfantry: return "LI";
                case UnitType.Militia: return "ML";
                case UnitType.Cavalry: return "CV";
                case UnitType.Artillery: return "AR";
                case UnitType.Grenadier: return "GR";
                case UnitType.Rifleman: return "RF";
                case UnitType.Hussar: return "HU";
                case UnitType.Cuirassier: return "CU";
                case UnitType.HorseArtillery: return "HA";
                case UnitType.Sapper: return "SP";
                case UnitType.OldGuard: return "OG";
                case UnitType.RocketBattery: return "RB";
                case UnitType.Lancer: return "LC";
                case UnitType.Dragoon: return "DG";
                default: return "??";
            }
        }

        /// <summary>
        /// Returns true if unit labels should be shown (colorblind mode is on).
        /// </summary>
        public bool ShouldShowUnitLabels => _colorblindMode;

        // --- Palette Builders ---

        private static Dictionary<string, ColorRgba> BuildNormalPalette()
        {
            return new Dictionary<string, ColorRgba>
            {
                // Unit ownership
                { "PlayerUnit", new ColorRgba(65, 105, 225) },    // Royal Blue
                { "EnemyUnit", new ColorRgba(220, 20, 60) },     // Crimson

                // Unit categories
                { "Infantry", new ColorRgba(100, 149, 237) },    // Cornflower Blue
                { "Cavalry", new ColorRgba(255, 215, 0) },       // Gold
                { "Artillery", new ColorRgba(178, 34, 34) },     // Firebrick
                { "Elite", new ColorRgba(148, 103, 189) },       // Purple

                // Terrain
                { "Terrain.OpenField", new ColorRgba(194, 178, 128) },  // Sand
                { "Terrain.Forest", new ColorRgba(34, 139, 34) },       // Forest Green
                { "Terrain.Hill", new ColorRgba(139, 119, 101) },       // Brown
                { "Terrain.River", new ColorRgba(65, 105, 225) },       // Blue
                { "Terrain.Bridge", new ColorRgba(160, 82, 45) },       // Sienna
                { "Terrain.Fortification", new ColorRgba(128, 128, 128) }, // Gray
                { "Terrain.Mud", new ColorRgba(139, 90, 43) },          // Dark Brown
                { "Terrain.Town", new ColorRgba(188, 143, 143) },       // Rosy Brown

                // UI
                { "UI.Positive", new ColorRgba(50, 205, 50) },   // Lime Green
                { "UI.Negative", new ColorRgba(255, 69, 0) },    // Red-Orange
                { "UI.Neutral", new ColorRgba(255, 255, 255) },  // White
                { "UI.Highlight", new ColorRgba(255, 215, 0) },  // Gold
            };
        }

        private static Dictionary<string, ColorRgba> BuildDeuteranopiaPalette()
        {
            // Red-green colorblindness: avoid red-green distinctions
            // Use blue-orange contrast instead
            return new Dictionary<string, ColorRgba>
            {
                { "PlayerUnit", new ColorRgba(0, 114, 178) },      // Blue
                { "EnemyUnit", new ColorRgba(230, 159, 0) },       // Orange

                { "Infantry", new ColorRgba(86, 180, 233) },       // Sky Blue
                { "Cavalry", new ColorRgba(240, 228, 66) },        // Yellow
                { "Artillery", new ColorRgba(213, 94, 0) },        // Vermillion
                { "Elite", new ColorRgba(204, 121, 167) },         // Reddish Purple

                { "Terrain.OpenField", new ColorRgba(194, 178, 128) },
                { "Terrain.Forest", new ColorRgba(0, 158, 115) },       // Bluish Green
                { "Terrain.Hill", new ColorRgba(139, 119, 101) },
                { "Terrain.River", new ColorRgba(0, 114, 178) },        // Blue
                { "Terrain.Bridge", new ColorRgba(160, 82, 45) },
                { "Terrain.Fortification", new ColorRgba(128, 128, 128) },
                { "Terrain.Mud", new ColorRgba(139, 90, 43) },
                { "Terrain.Town", new ColorRgba(188, 143, 143) },

                { "UI.Positive", new ColorRgba(0, 158, 115) },     // Bluish Green
                { "UI.Negative", new ColorRgba(213, 94, 0) },      // Vermillion
                { "UI.Neutral", new ColorRgba(255, 255, 255) },
                { "UI.Highlight", new ColorRgba(240, 228, 66) },   // Yellow
            };
        }

        private static Dictionary<string, ColorRgba> BuildTritanopiaPalette()
        {
            // Blue-yellow colorblindness: avoid blue-yellow distinctions
            // Use red-cyan contrast instead
            return new Dictionary<string, ColorRgba>
            {
                { "PlayerUnit", new ColorRgba(0, 180, 180) },      // Cyan
                { "EnemyUnit", new ColorRgba(220, 50, 50) },       // Red

                { "Infantry", new ColorRgba(100, 200, 200) },      // Light Cyan
                { "Cavalry", new ColorRgba(255, 150, 50) },        // Orange
                { "Artillery", new ColorRgba(180, 30, 30) },       // Dark Red
                { "Elite", new ColorRgba(200, 100, 200) },         // Magenta

                { "Terrain.OpenField", new ColorRgba(194, 178, 128) },
                { "Terrain.Forest", new ColorRgba(0, 150, 100) },       // Teal
                { "Terrain.Hill", new ColorRgba(139, 119, 101) },
                { "Terrain.River", new ColorRgba(0, 140, 180) },        // Dark Cyan
                { "Terrain.Bridge", new ColorRgba(160, 82, 45) },
                { "Terrain.Fortification", new ColorRgba(128, 128, 128) },
                { "Terrain.Mud", new ColorRgba(139, 90, 43) },
                { "Terrain.Town", new ColorRgba(188, 143, 143) },

                { "UI.Positive", new ColorRgba(0, 200, 150) },     // Teal Green
                { "UI.Negative", new ColorRgba(220, 50, 50) },     // Red
                { "UI.Neutral", new ColorRgba(255, 255, 255) },
                { "UI.Highlight", new ColorRgba(255, 150, 50) },   // Orange
            };
        }
    }
}
