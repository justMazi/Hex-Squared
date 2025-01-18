namespace Domain.Players;

public abstract class Player(int playerNum)
{
    public int PlayerNum { get; } = playerNum;
    public int NumberOfWins { get; set; } = 0;
    public bool GaveUp { get; set; } = false;
}