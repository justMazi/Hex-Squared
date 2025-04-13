namespace Domain.Players;

public class PathFinderHeuristic(int playerNum) : AiPlayer(playerNum)
{
    
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        return HeuristicHelper.PathFinderHeuristic(game, playerNum, cancellationToken);
    }
}
