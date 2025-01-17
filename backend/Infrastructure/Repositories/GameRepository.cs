using System.Collections.Concurrent;
using Application;
using Application.IRepositories;
using Domain;
using Domain.Players;
using Infrastructure.Exceptions;
using Serilog;

namespace Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private ConcurrentDictionary<GameId, Game?> Games { get; set; } = new();

    public List<Game?> GetAllInProgressGamesWithAi()
    {
        // gets all in progress games with at least one ai player
        return Games.Values.Where(g => g.GameState == GameState.InProgress && g.Players.Any(p => p is AiPlayer)).ToList();
    }

    public bool SaveGame(Game game)
    {
        return Games.TryUpdate(game.Id, game, Games.GetValueOrDefault(game.Id));
    }

    public void DeleteGame(Game existingGame)
    {
        Games.TryRemove(existingGame.Id, out var _);
    }

    public Game? GetById(GameId gameId)
    {
        return Games.GetValueOrDefault(gameId);
    }

    public Game CreateNewGame(GameId gameId, int radius)
    {
        var game =  new Game(gameId, radius);
        if (!Games.TryAdd(gameId, game))
        {
            throw new CantAddGameException(gameId);
        }
        Log.Information("Creating new game with id {gameId}", gameId);
        return game;
    }
    
}