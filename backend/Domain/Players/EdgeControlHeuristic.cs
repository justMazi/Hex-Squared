namespace Domain.Players;

public class EdgeControlHeuristic(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        var bestMove = game.PlayableHexagons
            .OrderByDescending(h => Math.Max(Math.Abs(h.Q), Math.Max(Math.Abs(h.R), Math.Abs(h.S))))
            .First();
        return Task.FromResult(bestMove.Index);
    }
}