namespace Domain.Players;

public class AiPlayer(int playerNum) : IPlayer
{
    public int PlayerNum { get; } = playerNum;
}



