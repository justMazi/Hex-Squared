namespace Domain.Players;

public class HumanPlayer(int playerNum) : IPlayer
{
    public int PlayerNum { get; } = playerNum;
}