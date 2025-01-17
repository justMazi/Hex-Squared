namespace Domain.Players;

public class RandomPlayer(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        return Task.FromResult(game.PlayableHexagons[Random.Shared.Next(game.PlayableHexagons.Count)].Index);
    }
}