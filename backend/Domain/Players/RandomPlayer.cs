namespace Domain.Players;

public class RandomPlayer(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        return HeuristicHelper.RandomPlayer(game, playerNum, cancellationToken);
    }
}