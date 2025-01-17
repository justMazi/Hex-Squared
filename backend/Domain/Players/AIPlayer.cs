namespace Domain.Players;

public abstract class AiPlayer(int playerNum) : IPlayer
{
    /// <summary>
    /// The player number associated with this AI player.
    /// </summary>
    public int PlayerNum { get; } = playerNum;
    public abstract Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken);
}

