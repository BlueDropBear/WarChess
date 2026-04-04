using System;
using System.Collections.Generic;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Developer-crafted armies that serve as opponents on day one when the ghost
    /// pool is empty. Each house army targets a specific Elo and tier.
    /// Pattern follows CampaignDatabase.cs.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class HouseArmyDatabase
    {
        /// <summary>Prefix used for house army player IDs.</summary>
        public const string HousePlayerIdPrefix = "house_";

        private static readonly List<HouseArmy> _armies;

        static HouseArmyDatabase()
        {
            _armies = new List<HouseArmy>();
            BuildTier1Armies();
            BuildTier2Armies();
            BuildTier3Armies();
            BuildTier4Armies();
            BuildTier5Armies();
        }

        /// <summary>Returns all house armies.</summary>
        public static IReadOnlyList<HouseArmy> AllArmies => _armies;

        /// <summary>Returns house armies for a specific tier.</summary>
        public static List<HouseArmy> GetArmiesForTier(int tier)
        {
            var result = new List<HouseArmy>();
            foreach (var a in _armies)
                if (a.Tier == tier) result.Add(a);
            return result;
        }

        /// <summary>
        /// Finds the house army closest to the target Elo at the given tier.
        /// Excludes any submission IDs in the exclude set.
        /// </summary>
        public static HouseArmy FindClosestArmy(int tier, int targetElo,
            HashSet<string> excludeIds = null)
        {
            HouseArmy best = null;
            int bestDiff = int.MaxValue;

            foreach (var a in _armies)
            {
                if (a.Tier != tier) continue;
                if (excludeIds != null && excludeIds.Contains(a.Submission.SubmissionId)) continue;

                int diff = Math.Abs(targetElo - a.TargetElo);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = a;
                }
            }

            return best;
        }

        /// <summary>
        /// Converts a HouseArmy to an ArmySubmission for battle resolution.
        /// </summary>
        public static ArmySubmission ToSubmission(HouseArmy army)
        {
            return army.Submission;
        }

        // ─── Tier 1: Brigadier (LineInfantry, Militia, Cavalry, Artillery) ───

        private static void BuildTier1Armies()
        {
            // Low Elo: heavy militia swarm
            AddArmy(1, 800, "Peasant Levy", new[]
            {
                U("Militia", 2, 9), U("Militia", 3, 9), U("Militia", 4, 9),
                U("Militia", 5, 9), U("Militia", 6, 9), U("Militia", 7, 9),
                U("Militia", 8, 9), U("LineInfantry", 5, 10)
            });

            // Mid-low: balanced infantry line
            AddArmy(1, 900, "Provincial Guard", new[]
            {
                U("LineInfantry", 3, 9), U("LineInfantry", 4, 9), U("LineInfantry", 5, 9),
                U("LineInfantry", 6, 9), U("LineInfantry", 7, 9),
                U("Artillery", 5, 10)
            });

            // Mid: classic line + cavalry flanker
            AddArmy(1, 1000, "Wellington's Line", new[]
            {
                U("LineInfantry", 3, 9), U("LineInfantry", 4, 9), U("LineInfantry", 5, 9),
                U("LineInfantry", 6, 9), U("Cavalry", 2, 8), U("Cavalry", 8, 8),
                U("Artillery", 5, 10)
            });

            // Mid-high: cavalry-heavy aggression
            AddArmy(1, 1100, "Dragoon Rush", new[]
            {
                U("Cavalry", 2, 8), U("Cavalry", 3, 8), U("Cavalry", 7, 8), U("Cavalry", 8, 8),
                U("LineInfantry", 4, 9), U("LineInfantry", 5, 9), U("LineInfantry", 6, 9)
            });

            // High: artillery-focused with screen
            AddArmy(1, 1200, "Grand Battery", new[]
            {
                U("Artillery", 4, 10), U("Artillery", 6, 10),
                U("LineInfantry", 3, 9), U("LineInfantry", 4, 9), U("LineInfantry", 5, 9),
                U("LineInfantry", 6, 9), U("LineInfantry", 7, 9)
            });

            // Expert: combined arms
            AddArmy(1, 1400, "Austrian Reserve", new[]
            {
                U("LineInfantry", 3, 9), U("LineInfantry", 5, 9), U("LineInfantry", 7, 9),
                U("Cavalry", 2, 8), U("Cavalry", 8, 8),
                U("Artillery", 4, 10), U("Artillery", 6, 10),
                U("Militia", 5, 10)
            });
        }

        // ─── Tier 2: Major General (+ Grenadier, Rifleman) ───

        private static void BuildTier2Armies()
        {
            AddArmy(2, 900, "Skirmish Screen", new[]
            {
                U("Rifleman", 3, 8), U("Rifleman", 5, 8), U("Rifleman", 7, 8),
                U("LineInfantry", 3, 9), U("LineInfantry", 5, 9), U("LineInfantry", 7, 9),
                U("Militia", 4, 10), U("Militia", 6, 10)
            });

            AddArmy(2, 1000, "Grenadier Assault", new[]
            {
                U("Grenadier", 4, 9), U("Grenadier", 5, 9), U("Grenadier", 6, 9),
                U("LineInfantry", 3, 10), U("LineInfantry", 7, 10),
                U("Artillery", 5, 10)
            });

            AddArmy(2, 1100, "Prussian Discipline", new[]
            {
                U("LineInfantry", 3, 9), U("LineInfantry", 4, 9), U("LineInfantry", 5, 9),
                U("LineInfantry", 6, 9), U("LineInfantry", 7, 9),
                U("Grenadier", 5, 10), U("Rifleman", 2, 8), U("Rifleman", 8, 8)
            });

            AddArmy(2, 1200, "Combined Advance", new[]
            {
                U("Grenadier", 4, 9), U("Grenadier", 6, 9),
                U("Rifleman", 3, 8), U("Rifleman", 7, 8),
                U("Cavalry", 2, 8), U("Cavalry", 8, 8),
                U("Artillery", 5, 10)
            });

            AddArmy(2, 1400, "Imperial Vanguard", new[]
            {
                U("Grenadier", 3, 9), U("Grenadier", 5, 9), U("Grenadier", 7, 9),
                U("Rifleman", 2, 8), U("Rifleman", 8, 8),
                U("Cavalry", 2, 9), U("Cavalry", 8, 9),
                U("Artillery", 5, 10)
            });

            AddArmy(2, 1600, "Russian Steamroller", new[]
            {
                U("Grenadier", 3, 9), U("Grenadier", 4, 9), U("Grenadier", 5, 9),
                U("Grenadier", 6, 9), U("Grenadier", 7, 9),
                U("Rifleman", 4, 8), U("Rifleman", 6, 8),
                U("Artillery", 5, 10)
            });
        }

        // ─── Tier 3: Lieutenant General (+ Hussar, Cuirassier, HorseArtillery) ───

        private static void BuildTier3Armies()
        {
            AddArmy(3, 1000, "Hussar Raid", new[]
            {
                U("Hussar", 2, 8), U("Hussar", 3, 8), U("Hussar", 8, 8),
                U("LineInfantry", 4, 9), U("LineInfantry", 5, 9), U("LineInfantry", 6, 9),
                U("Artillery", 5, 10)
            });

            AddArmy(3, 1200, "Heavy Cavalry Corps", new[]
            {
                U("Cuirassier", 3, 8), U("Cuirassier", 5, 8), U("Cuirassier", 7, 8),
                U("Hussar", 2, 8), U("Hussar", 8, 8),
                U("HorseArtillery", 5, 9)
            });

            AddArmy(3, 1400, "Murat's Charge", new[]
            {
                U("Cuirassier", 4, 8), U("Cuirassier", 6, 8),
                U("Hussar", 2, 8), U("Hussar", 8, 8),
                U("Grenadier", 5, 9),
                U("HorseArtillery", 3, 10), U("HorseArtillery", 7, 10)
            });

            AddArmy(3, 1600, "Combined Arms Corps", new[]
            {
                U("Grenadier", 4, 9), U("Grenadier", 6, 9),
                U("Rifleman", 3, 8), U("Rifleman", 7, 8),
                U("Cuirassier", 2, 8), U("Cuirassier", 8, 8),
                U("HorseArtillery", 5, 10), U("Artillery", 5, 9)
            });

            AddArmy(3, 1800, "Grand Armée Vanguard", new[]
            {
                U("Cuirassier", 3, 8), U("Cuirassier", 7, 8),
                U("Hussar", 2, 8), U("Hussar", 8, 8),
                U("Grenadier", 4, 9), U("Grenadier", 5, 9), U("Grenadier", 6, 9),
                U("HorseArtillery", 5, 10)
            });
        }

        // ─── Tier 4: General (+ Sapper, Lancer, Dragoon) ───

        private static void BuildTier4Armies()
        {
            AddArmy(4, 1200, "Siege Corps", new[]
            {
                U("Sapper", 4, 9), U("Sapper", 6, 9),
                U("Artillery", 4, 10), U("Artillery", 6, 10),
                U("LineInfantry", 3, 9), U("LineInfantry", 5, 9), U("LineInfantry", 7, 9)
            });

            AddArmy(4, 1400, "Lancer Screen", new[]
            {
                U("Lancer", 2, 8), U("Lancer", 4, 8), U("Lancer", 6, 8), U("Lancer", 8, 8),
                U("Dragoon", 3, 9), U("Dragoon", 7, 9),
                U("HorseArtillery", 5, 10)
            });

            AddArmy(4, 1600, "Fortified Position", new[]
            {
                U("Sapper", 3, 9), U("Sapper", 7, 9),
                U("Grenadier", 4, 9), U("Grenadier", 5, 9), U("Grenadier", 6, 9),
                U("Rifleman", 2, 8), U("Rifleman", 8, 8),
                U("Artillery", 5, 10)
            });

            AddArmy(4, 1800, "Ney's Advance", new[]
            {
                U("Dragoon", 3, 8), U("Dragoon", 7, 8),
                U("Lancer", 2, 8), U("Lancer", 8, 8),
                U("Cuirassier", 5, 8),
                U("Grenadier", 4, 9), U("Grenadier", 6, 9),
                U("HorseArtillery", 5, 10)
            });

            AddArmy(4, 2000, "Davout's Iron Division", new[]
            {
                U("Grenadier", 3, 9), U("Grenadier", 5, 9), U("Grenadier", 7, 9),
                U("Sapper", 4, 9), U("Sapper", 6, 9),
                U("Dragoon", 2, 8), U("Dragoon", 8, 8),
                U("Lancer", 3, 8), U("Lancer", 7, 8),
                U("Artillery", 5, 10)
            });
        }

        // ─── Tier 5: Marshal of the Empire (+ OldGuard, RocketBattery) ───

        private static void BuildTier5Armies()
        {
            AddArmy(5, 1400, "Rocket Corps", new[]
            {
                U("RocketBattery", 4, 10), U("RocketBattery", 6, 10),
                U("LineInfantry", 3, 9), U("LineInfantry", 5, 9), U("LineInfantry", 7, 9),
                U("Sapper", 4, 9), U("Sapper", 6, 9)
            });

            AddArmy(5, 1600, "Old Guard March", new[]
            {
                U("OldGuard", 3, 9), U("OldGuard", 5, 9), U("OldGuard", 7, 9),
                U("Grenadier", 4, 10), U("Grenadier", 6, 10),
                U("Artillery", 5, 10)
            });

            AddArmy(5, 1800, "Emperor's Finest", new[]
            {
                U("OldGuard", 4, 9), U("OldGuard", 5, 9), U("OldGuard", 6, 9),
                U("Cuirassier", 2, 8), U("Cuirassier", 8, 8),
                U("RocketBattery", 5, 10), U("HorseArtillery", 3, 10)
            });

            AddArmy(5, 2000, "Waterloo", new[]
            {
                U("OldGuard", 3, 9), U("OldGuard", 5, 9), U("OldGuard", 7, 9),
                U("Cuirassier", 2, 8), U("Cuirassier", 8, 8),
                U("Lancer", 3, 8), U("Lancer", 7, 8),
                U("RocketBattery", 4, 10), U("Artillery", 6, 10)
            });

            AddArmy(5, 2200, "Grand Armée Supreme", new[]
            {
                U("OldGuard", 3, 9), U("OldGuard", 4, 9), U("OldGuard", 5, 9),
                U("OldGuard", 6, 9), U("OldGuard", 7, 9),
                U("Cuirassier", 2, 8), U("Cuirassier", 8, 8),
                U("RocketBattery", 5, 10), U("HorseArtillery", 4, 10)
            });
        }

        // ─── Helpers ───

        private static SubmittedUnit U(string unitType, int x, int y)
        {
            return new SubmittedUnit { UnitTypeId = unitType, X = x, Y = y, OfficerId = null };
        }

        private static void AddArmy(int tier, int targetElo, string name, SubmittedUnit[] units)
        {
            string armyId = $"house_t{tier}_{name.Replace(" ", "").Replace("'", "")}";

            var submission = new ArmySubmission
            {
                SubmissionId = armyId,
                PlayerId = HousePlayerIdPrefix + armyId,
                ArmyName = name,
                Tier = tier,
                TotalCost = 0, // House armies bypass budget validation
                CommanderId = null,
                CommanderActivationRound = 0,
                SubmittedAtTicks = 0,
                Units = new List<SubmittedUnit>(units),
                Status = SubmissionStatus.InPool
            };

            _armies.Add(new HouseArmy
            {
                Tier = tier,
                TargetElo = targetElo,
                Submission = submission
            });
        }
    }

    /// <summary>
    /// A developer-crafted army with a target Elo rating for matchmaking.
    /// </summary>
    [Serializable]
    public class HouseArmy
    {
        /// <summary>Tier this army is designed for.</summary>
        public int Tier;

        /// <summary>Target Elo for matchmaking proximity.</summary>
        public int TargetElo;

        /// <summary>The army submission data.</summary>
        public ArmySubmission Submission;
    }
}
