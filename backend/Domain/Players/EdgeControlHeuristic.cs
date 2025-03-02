namespace Domain.Players;

public class EdgeControlHeuristic(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        return HeuristicHelper.EdgeControlHeuristic(game, playerNum, cancellationToken);
    }
}