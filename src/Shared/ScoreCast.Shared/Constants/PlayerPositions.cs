namespace ScoreCast.Shared.Constants;

public static class PlayerPositions
{
    public const string Goalkeeper = "Goalkeeper";
    public const string CentreBack = "Centre-Back";
    public const string LeftBack = "Left-Back";
    public const string RightBack = "Right-Back";
    public const string Defence = "Defence";
    public const string DefensiveMidfield = "Defensive Midfield";
    public const string CentralMidfield = "Central Midfield";
    public const string AttackingMidfield = "Attacking Midfield";
    public const string LeftMidfield = "Left Midfield";
    public const string RightMidfield = "Right Midfield";
    public const string Midfield = "Midfield";
    public const string LeftWinger = "Left Winger";
    public const string RightWinger = "Right Winger";
    public const string CentreForward = "Centre-Forward";
    public const string Offence = "Offence";

    private static readonly Dictionary<string, string> _shortNames = new()
    {
        [Goalkeeper] = "GK",
        [CentreBack] = "CB",
        [LeftBack] = "LB",
        [RightBack] = "RB",
        [Defence] = "CB",
        [DefensiveMidfield] = "DM",
        [CentralMidfield] = "CM",
        [AttackingMidfield] = "AM",
        [LeftMidfield] = "LM",
        [RightMidfield] = "RM",
        [Midfield] = "CM",
        [LeftWinger] = "LW",
        [RightWinger] = "RW",
        [CentreForward] = "CF",
        [Offence] = "CF"
    };

    public static string ToShortName(string? position) =>
        position is not null && _shortNames.TryGetValue(position, out var s) ? s : "—";

    public static string ToGroupName(string? position) => position switch
    {
        Goalkeeper => "Goalkeeper",
        CentreBack or LeftBack or RightBack or Defence => "Defender",
        DefensiveMidfield or CentralMidfield or AttackingMidfield
            or LeftMidfield or RightMidfield or Midfield => "Midfielder",
        LeftWinger or RightWinger or CentreForward or Offence => "Attacker",
        _ => ""
    };
}
