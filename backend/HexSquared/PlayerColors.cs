using System.Collections.Concurrent;

namespace HexSquared;

public static class Colors
{
    public static string PlayerToColor(PlayerEnum playerEnum)
    {
        return playerEnum switch
        {
            PlayerEnum.None => NoneColor,   // Non taken field gray color
            PlayerEnum.Player1 => Player1Color,
            PlayerEnum.Player2 => Player2Color,
            PlayerEnum.Player3 => Player3Color,
            _ => throw new ArgumentOutOfRangeException(nameof(playerEnum), playerEnum, null)
        };
    }

    public static readonly string NoneColor = "#b3b3b3";
    public static readonly string Player1Color = "#3ebb40";
    public static readonly string Player2Color = "#c03030";
    public static readonly string Player3Color = "#2f43b0";
}