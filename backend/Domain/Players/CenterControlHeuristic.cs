namespace Domain.Players;

public class CenterControlHeuristic(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        var bestMove = game.PlayableHexagons
            .OrderBy(h => Math.Abs(h.Q) + Math.Abs(h.R) + Math.Abs(h.S))
            .First();
        return Task.FromResult(bestMove.Index);
    }
}