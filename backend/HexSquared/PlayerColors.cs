using System.Collections.Concurrent;

namespace HexSquared;

public static class Colors
{
    public static string PlayerToColor(Player player)
    {
        return player switch
        {
            Player.None => NoneColor,   // Non taken field gray color
            Player.Player1 => Player1Color,
            Player.Player2 => Player2Color,
            Player.Player3 => Player3Color,
            _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
        };
    }

    public static readonly string NoneColor = "#b3b3b3";
    public static readonly string Player1Color = "#3ebb40";
    public static readonly string Player2Color = "#c03030";
    public static readonly string Player3Color = "#2f43b0";
}