using System.ComponentModel.DataAnnotations;

namespace HexSquared;

public class Hex
{
    public Hex(int r, int s, int q, int index, int player = 0)
    {
        R = r;
        S = s;
        Q = q;
        Index = index;
        Player = player;
    }

    public int R { get; }
    public int S { get; }
    public int Q { get; }
    public int Index { get; }
    public int Player { get; private set; }
    
    public void SetPlayer(int player)
    {
        Player = player;
    }
}

