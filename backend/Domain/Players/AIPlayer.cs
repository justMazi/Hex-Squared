namespace Domain.Players;

public abstract class AiPlayer(int playerNum) : Player(playerNum)
{
    public abstract Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken);
}


