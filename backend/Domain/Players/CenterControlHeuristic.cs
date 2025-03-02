namespace Domain.Players;

public class CenterControlHeuristic(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        return HeuristicHelper.CenterControlHeuristic(game, playerNum, cancellationToken);
    }
}