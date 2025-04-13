namespace Domain;

public record Hex(int R, int S, int Q, int Index, int Owner = 0)
{
    public bool IsTaken => Owner != 0;

    public Hex SetPlayer(int player)
    {
        // Return a new Hex with updated Owner
        return this with { Owner = player };
    }
}