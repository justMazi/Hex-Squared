using System.ComponentModel.DataAnnotations;

namespace HexSquared.Dto;

public class Hex
{
    [Required]
    public int R { get; init; }
    
    [Required]
    public int S { get; init; }
    
    [Required]
    public int Q { get; init; }
    
    [Required]
    public int Index { get; init; }
    
    [Required]
    [EnumDataType(typeof(Player))]
    public int Player { get; init; }
}