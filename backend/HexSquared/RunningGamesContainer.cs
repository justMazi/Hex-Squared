using System.Collections.Concurrent;

namespace HexSquared;

public class RunningGamesContainer
{
    private ConcurrentDictionary<string, Game> Games { get; set; } = new();

    public Game GetState(string gameCode)
    {
        return Games.GetOrAdd(gameCode, new Game(gameCode));
    }
}