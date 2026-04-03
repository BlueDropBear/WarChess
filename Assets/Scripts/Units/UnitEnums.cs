namespace WarChess.Units
{
    /// <summary>
    /// All 14 unit types in the game. Order matches GDD unlock progression.
    /// </summary>
    public enum UnitType
    {
        LineInfantry,
        Militia,
        Cavalry,
        Artillery,
        Grenadier,
        Rifleman,
        Hussar,
        Cuirassier,
        HorseArtillery,
        Sapper,
        OldGuard,
        RocketBattery,
        Lancer,
        Dragoon
    }

    /// <summary>
    /// Targeting AI behavior for selecting which enemy to engage.
    /// </summary>
    public enum TargetingPriority
    {
        Nearest,
        Weakest,
        HighestThreat,
        ArtilleryFirst,
        Random
    }

    /// <summary>
    /// Direction a unit faces. Player units face North (toward row 10),
    /// enemy units face South (toward row 1).
    /// </summary>
    public enum FacingDirection
    {
        North,
        South
    }

    /// <summary>
    /// Which side of the battle a unit belongs to.
    /// </summary>
    public enum Owner
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Special abilities unique to each unit type.
    /// </summary>
    public enum AbilityType
    {
        None,
        StrengthInNumbers,
        Charge,
        Bombardment,
        Grenade,
        AimedShot,
        HitAndRun,
        ArmoredCharge,
        LimberedUp,
        Entrench,
        Unbreakable,
        CongreveBarrage,
        Brace,
        Dismount
    }

    /// <summary>
    /// Formation types that grant bonuses when unit arrangements are detected.
    /// </summary>
    public enum FormationType
    {
        None,
        BattleLine,
        Battery,
        CavalryWedge,
        Square,
        SkirmishScreen
    }

    /// <summary>
    /// Direction of an attack relative to the defender's facing.
    /// </summary>
    public enum FlankDirection
    {
        Front,
        Side,
        Rear
    }
}
